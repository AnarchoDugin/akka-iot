using Akka.Actor;
using Akka.Event;
using DeviceSystemBackend.Messages;
using Npgsql;


namespace DeviceSystemBackend.Actors.Database
{
    public class DatabaseQueryActor : ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger();
        private readonly NpgsqlDataSource _dataSource;

        protected override void PreStart() => _logger.Info("Database Actor Pool Routee Started");
        protected override void PostStop() => _logger.Info("Database Actor Pool Routee Stopped");

        public DatabaseQueryActor(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;

            Receive<UserMessages.RegisterUser>(HandleRegisterUser);
            Receive<UserMessages.Login>(HandleLogin);
            Receive<UserMessages.DeleteUser>(HandleDeleteUser);
            
            Receive<DeviceMessages.RegisterDevice>(HandleRegisterDevice);
            Receive<DeviceMessages.UnregisterDevice>(HandleUnregisterDevice);
            Receive<DeviceMessages.RequestDeviceList>(HandleDeviceListRequest);
            Receive<DeviceMessages.CheckDeviceExists>(HandleDeviceCheck);
        }

        private void HandleRegisterUser(UserMessages.RegisterUser msg)
        {
            RegisterUserAsync(msg).PipeTo(
                recipient: Sender,
                sender: Self,
                success: registered => registered,
                failure: ex => HandleError(ex, $"User registration failed for user: {msg.Username}")
            );
        }

        private async Task<object> RegisterUserAsync(UserMessages.RegisterUser msg)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            if (await UserExistsAsync(connection, transaction, msg.Username))
            {
                await transaction.RollbackAsync();
                _logger.Warning($"User already exists: {msg.Username}");
                return new ErrorMessages.UserAlreadyExists();
            }

            await CreateUserAsync(connection, transaction, msg);
            await transaction.CommitAsync();
            
            _logger.Info($"User registered: {msg.Username}");
            return new UserMessages.UserRegistered();
        }

        private void HandleLogin(UserMessages.Login msg)
        {
            LoginUserAsync(msg).PipeTo(
                recipient: Sender,
                sender: Self,
                success: result => result,
                failure: ex => HandleError(ex, $"Login failed for user:  {msg.Username}")
            );
        }

        private async Task<object> LoginUserAsync(UserMessages.Login msg)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            var passwordHash = await GetPasswordHashAsync(connection, msg.Username);

            if (passwordHash == null)
            {
                _logger.Warning($"User not found: {msg.Username}");
                return new ErrorMessages.UserDoesNotExist();
            }

            if (!BCrypt.Net.BCrypt.Verify(msg.Password, passwordHash))
            {
                _logger.Warning($"Invalid password for: {msg.Username}");
                return new ErrorMessages.LoginFailed();
            }

            _logger.Info($"User logged in: {msg.Username}");
            return new UserMessages.LoginCompleted();
        }

        private void HandleDeleteUser(UserMessages.DeleteUser msg)
        {
            DeleteUserAsync(msg).PipeTo(
                recipient: Sender,
                sender: Self,
                success: result => result,
                failure: ex => HandleError(ex, $"Failed to delete user: {msg.Username}")
            );
        }

        private async Task<object> DeleteUserAsync(UserMessages.DeleteUser msg)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            var deleted = await DeleteUserFromDbAsync(connection, msg.Username);

            if (!deleted)
                return new ErrorMessages.UserDoesNotExist();

            _logger.Info($"User deleted: {msg.Username}");
            return new UserMessages.UserDeleted();
        }

        private void HandleRegisterDevice(DeviceMessages.RegisterDevice msg)
        {
            RegisterDeviceAsync(msg).PipeTo(
                recipient: Sender,
                sender: Self,
                success: result => result,
                failure: ex => HandleError(ex, $"Device registration failed for {msg.Name}")
            );
        }

        private async Task<object> RegisterDeviceAsync(DeviceMessages.RegisterDevice msg)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            if (await DeviceExistsAsync(connection, transaction, msg.Name))
            {
                await transaction.RollbackAsync();
                _logger.Warning($"Device already exists: {msg.Name}");
                return new ErrorMessages.DeviceAlreadyExists();
            }

            await CreateDeviceAsync(connection, transaction, msg);
            await transaction.CommitAsync();
            
            _logger.Info($"Device registered: {msg.Name}");
            return new DeviceMessages.DeviceRegistered();
        }

        private void HandleUnregisterDevice(DeviceMessages.UnregisterDevice msg)
        {
            UnregisterDeviceAsync(msg).PipeTo(
                recipient: Sender,
                sender: Self,
                success: result => result,
                failure: ex => HandleError(ex, $"Device unregistration failed for {msg.Name}")
            );
        }

        private async Task<object> UnregisterDeviceAsync(DeviceMessages.UnregisterDevice msg)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            var deleted = await DeleteDeviceFromDbAsync(connection, msg.Name);

            if (!deleted)
                return new ErrorMessages.DeviceDoesNotExist();

            _logger.Info($"Device unregistered: {msg.Name}");
            return new DeviceMessages.DeviceUnregistered();
        }

        private void HandleDeviceListRequest(DeviceMessages.RequestDeviceList msg)
        {
            GetDeviceListAsync().PipeTo(
                recipient: Sender,
                sender: Self,
                success: devices => new DeviceMessages.RespondDeviceList(devices),
                failure: ex => HandleError(ex, "Failed to fetch device list")
            );
        }

        private async Task<IList<object>> GetDeviceListAsync()
        {
            var devices = new List<object>();
            await using var connection = await _dataSource.OpenConnectionAsync();
            
            const string sql = "SELECT device_name, device_serial_number, device_location FROM devices";
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                devices.Add(new
                {
                    name = reader.GetString(0),
                    serialNumber = reader.GetString(1),
                    location = reader.GetString(2)
                });
            }

            _logger.Info($"Fetched {devices.Count} devices from database");
            return devices;
        }

        private void HandleDeviceCheck(DeviceMessages.CheckDeviceExists msg)
        {
            DeviceCheckAsync(msg).PipeTo(
                recipient: Sender,
                sender: Self,
                success: response => response,
                failure: ex => HandleError(ex, $"Device check failed for {msg.DeviceName}")
                );
        }

        private async Task<object> DeviceCheckAsync(DeviceMessages.CheckDeviceExists msg)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            
            if (await DeviceExistsAsync(connection, transaction, msg.DeviceName))
            {
                return new DeviceMessages.DeviceChecked();
            }

            return new ErrorMessages.DeviceDoesNotExist();
        }
        
        private async Task<bool> UserExistsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string username)
        {
            const string sql = "SELECT COUNT(1) FROM users WHERE username = @username";
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("username", username);
            return (long)(await command.ExecuteScalarAsync() ?? 0) > 0;
        }

        private async Task CreateUserAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, 
            UserMessages.RegisterUser msg)
        {
            const string sql = "INSERT INTO users (username, password_hash) VALUES (@username, @password)";
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("username", msg.Username);
            command.Parameters.AddWithValue("password", BCrypt.Net.BCrypt.HashPassword(msg.Password));
            await command.ExecuteNonQueryAsync();
        }

        private async Task<string?> GetPasswordHashAsync(NpgsqlConnection connection, string username)
        {
            const string sql = "SELECT password_hash FROM users WHERE username = @username";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("username", username);
            return (await command.ExecuteScalarAsync())?.ToString();
        }

        private async Task<bool> DeleteUserFromDbAsync(NpgsqlConnection connection, string username)
        {
            const string sql = "DELETE FROM users WHERE username = @username";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("username", username);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        private async Task<bool> DeviceExistsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string deviceName)
        {
            const string sql = "SELECT COUNT(1) FROM devices WHERE device_name = @name";
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("name", deviceName);
            return (long)(await command.ExecuteScalarAsync() ?? 0) > 0;
        }

        private async Task CreateDeviceAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
            DeviceMessages.RegisterDevice msg)
        {
            const string sql = "INSERT INTO devices (device_name, device_serial_number, device_location) VALUES (@name, @serialNumber, @location)";
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("name", msg.Name);
            command.Parameters.AddWithValue("serialNumber", msg.SerialNumber);
            command.Parameters.AddWithValue("location", msg.Location);
            await command.ExecuteNonQueryAsync();
        }

        private async Task<bool> DeleteDeviceFromDbAsync(NpgsqlConnection connection, string deviceName)
        {
            const string sql = "DELETE FROM devices WHERE device_name = @name";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("name", deviceName);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        
        private IErrorMessage HandleError(Exception ex, string context)
        {
            _logger.Error(ex, context);
            return ex is NpgsqlException 
                ? new ErrorMessages.DatabaseError() 
                : new ErrorMessages.UnexpectedError();
        }

        public static Props Props(NpgsqlDataSource dataSource) => 
            Akka.Actor.Props.Create(() => new DatabaseQueryActor(dataSource));
    }
}