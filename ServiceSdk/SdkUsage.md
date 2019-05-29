# Kokka Koro Bot SDK

### Basic Concepts
There are two types of communication to the server. 

- Requests / Response
- Broadcast Messages

For every request sent there should always be a response. If a response is not received, the websocket is in an unknown state and will be disconnected. If the websocket is every disconnected for any reason, you must create a new Service object and connect again. Reconnection logic is not handled in the SDK.

### Requests / Response

Any command or action taken by the bot is a request, which will always be replied to with a response. It is allowed to send multiple requests at once, each will get a response.

### Broadcast Messages

A broadcast message is any message that was not sent to the client due to a request. These types of messages are typically sent to many bots and players, updating things like game state and when new games are created.

### Websocket Protocol

Json messages are sent over the websocket to the server. The json messages are expressed by C# classes in the ServiceProtocol project.

