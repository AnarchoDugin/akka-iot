function DashboardApp() {
    return {
        ApplicationVariables: {
            WebSocket: null
        },

        ApplicationState: {
            isAuthenticated: false,
            isWebSocketConnected: false,
        },

        UserCredentials: {
            Username: '',
            Password: ''
        },

        ApiServer: {
            HttpConnectionConfiguration: {
                ConnectionProtocol: "https",
                ConnectionDomain: "localhost",
                ConnectionPort: "7136"
            },

            WebSocketConnectionConfiguration: {
                ConnectionProtocol: "wss",
                ConnectionDomain: "localhost",
                ConnectionPort: "7136"
            },

            ApiRoutingConfiguration: {
                UserAuthentification: "/api/v1/user/auth",
                DeviceListRequest: "/api/v1/device/list",
                WebSocketRoute: "/ws"
            }
        },

        ErrorMessages: { ApplicationError: null },

        DeviceList: [],

        async AuthenticateUser() {
            const Username = this.UserCredentials.Username;
            const Password = this.UserCredentials.Password;
            if (Username && Password) {
                await this.PerformServerAuthentification();
            }
        },

        EndUserSession() {
            const Socket = this.ApplicationVariables.WebSocket;
            if (Socket)
                Socket.close();
                this.ApplicationVariables.WebSocket = null;
            this.ApplicationState.isAuthenticated = false;
            this.ApplicationState.isWebSocketConnected = false;
        },

        ConnectToServer() {
            const ConnectionString = this.ForgeConnectionString("WebSocketConnection");
            const WebSocketApiRoute = this.ApiServer.ApiRoutingConfiguration.WebSocketRoute;
            const WebSocketConnectionPath = ConnectionString + WebSocketApiRoute;
            const ConnectionSocket = new WebSocket(WebSocketConnectionPath);

            ConnectionSocket.addEventListener("open", () => {
                console.log("Connection with the server establised!");
            });

            ConnectionSocket.addEventListener("message", (message) => {
                const MessageData = message.data;
                console.log("Received a message from the socket:", MessageData);
                this.ProcessWebSocketMessage(MessageData);
            });

            ConnectionSocket.addEventListener("error", (error) => {
                console.error("WebSocket error:", error);
            });

            ConnectionSocket.addEventListener("close", () => {
                console.log("Connection with the server closed!");
            });

            this.ApplicationVariables.WebSocket = ConnectionSocket;
            this.ApplicationState.isWebSocketConnected = true;
        },

        async PerformServerAuthentification() {
            const ConnectionString = this.ForgeConnectionString("HttpConnection");
            const AuthentificationRoute = this.ApiServer.ApiRoutingConfiguration.UserAuthentification;

            const AuthentificationPath = ConnectionString + AuthentificationRoute;
            const AuthentificationRequest = new Request(AuthentificationPath);
            try {
                const AuthentificationResponse = await fetch(AuthentificationRequest, {
                    headers: { 
                        "Content-Type": "application/json",
                        "accept": "application/json"
                    },
                    method: "POST",
                    body: JSON.stringify({ 
                        "username": this.UserCredentials.Username,
                        "password": this.UserCredentials.Password
                    })
                });

                const ResponseData = await AuthentificationResponse.json();
                if (ResponseData.status === 200) {
                    this.ApplicationState.isAuthenticated = true;
                    await this.RequestDeviceList();
                    this.ConnectToServer();
                }

                else {
                    const ErrorMessage = "Could not authentificate the user. Check the credentials again...";
                    this.ErrorMessages.ApplicationError = ErrorMessage;
                }

            } catch (error) {
                console.error("An error occured:", error.message);
            }
        },

        async RequestDeviceList() {
            const ConnectionString = this.ForgeConnectionString("HttpConnection");
            const DeviceListRoute = this.ApiServer.ApiRoutingConfiguration.DeviceListRequest;

            const DeviceListRequestPath = ConnectionString + DeviceListRoute;
            const DeviceListRequest = new Request(DeviceListRequestPath);
            try {
                const DeviceListResponse = await fetch(DeviceListRequest, {
                    method: "GET",
                    headers: { "accept": "application/json" }
                });

                const DeviceListJson = await DeviceListResponse.json();
                const DeviceEntries = DeviceListJson.data;

                for (const Device of DeviceEntries) {
                    const DeviceItem = {};

                    DeviceItem.Name = Device.name;
                    DeviceItem.Status = "Offline";
                    DeviceItem.Temperature = "N/A";
                    DeviceItem.Humidity = "N/A"
                    DeviceItem.Uptime = "N/A";

                    if (!this.DeviceList.includes(DeviceItem))
                        this.DeviceList.push(DeviceItem);
                }

            } catch (error) {
                console.error("An error occured:", error.message);
            }
        },

        ProcessWebSocketMessage(message) {
            const MessageJson = JSON.parse(message);
            const DeviceItem = {};

            DeviceItem.Name = MessageJson["deviceName"];
            DeviceItem.Temperature = parseFloat(MessageJson["temperature"]).toFixed(2);
            DeviceItem.Humidity = parseFloat(MessageJson["humidity"]).toFixed(2);
            DeviceItem.Status = "Online";
            DeviceItem.Uptime = this.FormatElapsedUptime(MessageJson["deviceUpTime"]);

            const DeviceIndex = this.DeviceList.findIndex(item => item.Name === DeviceItem.Name);
            if (DeviceIndex != -1) {
                const InitialDevice = this.DeviceList[DeviceIndex];
                InitialDevice.Temperature = DeviceItem.Temperature;
                InitialDevice.Humidity = DeviceItem.Humidity;
                InitialDevice.Status = "Online";
                InitialDevice.Uptime = DeviceItem.Uptime;
            }

            else {
                this.DeviceList.push(DeviceItem);
            }
        },

        ForgeConnectionString(ConnectionType) {
            let ConnectionString = null;
            switch (ConnectionType) {
                case "HttpConnection":
                    const HttpProtocol = this.ApiServer.HttpConnectionConfiguration.ConnectionProtocol;
                    const HttpServerDomain = this.ApiServer.HttpConnectionConfiguration.ConnectionDomain;
                    const HttpApiServerPort = this.ApiServer.HttpConnectionConfiguration.ConnectionPort;
                    ConnectionString = HttpProtocol + "://" + HttpServerDomain + ":" + HttpApiServerPort;
                    break;

                case "WebSocketConnection":
                    const WebSocketProtocol = this.ApiServer.WebSocketConnectionConfiguration.ConnectionProtocol;
                    const WebSocketServerDomain = this.ApiServer.WebSocketConnectionConfiguration.ConnectionDomain;
                    const WebSocketApiServerPort = this.ApiServer.WebSocketConnectionConfiguration.ConnectionPort;
                    ConnectionString = WebSocketProtocol + "://" + WebSocketServerDomain + ":" + WebSocketApiServerPort;
                    break;
    
                default:
                    throw new Error("Invalid connection string.");
            }
            return ConnectionString;
        },

        FormatElapsedUptime(TimeString) {
            const [timePart, milliseconds] = TimeString.split('.');
            const [hours, minutes, seconds] = timePart.split(':').map(Number);
            if (hours >= 24) {
                const days = Math.floor(hours / 24);
                return `${days}d`;  
            }
            else if (hours > 0) {
                return `${hours}h`;        
            }
            else if (minutes > 0) {
                return `${minutes}m`;      
            }
            else {
                return `${seconds}s`;               
            }
        }  
    }
}