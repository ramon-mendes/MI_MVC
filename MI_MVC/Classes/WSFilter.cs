using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.WebSockets;

namespace MI_MVC.Classes
{
	public class WSFilter : AuthorizeAttribute
	{
		protected override bool AuthorizeCore(HttpContextBase httpContext)
		{
			if(httpContext.IsWebSocketRequest)
			{
				httpContext.AcceptWebSocketRequest(WebSocketRequestHandler);
				return true;
			}
			return true;
		}

		public async Task WebSocketRequestHandler(AspNetWebSocketContext webSocketContext)
		{
			WebSocket webSocket = webSocketContext.WebSocket;

			/*We define a certain constant which will represent
            size of received data. It is established by us and 
            we can set any value. We know that in this case the size of the sent
            data is very small.
            */
			const int maxMessageSize = 1024;

			//Buffer for received bits.
			var receivedDataBuffer = new ArraySegment<Byte>(new Byte[maxMessageSize]);
			var cancellationToken = new CancellationToken();

			while(webSocket.State == WebSocketState.Open)
			{
				//Reads data.
				WebSocketReceiveResult webSocketReceiveResult = await webSocket.ReceiveAsync(receivedDataBuffer, cancellationToken);

				//If input frame is cancelation frame, send close command.
				if(webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
				{
					await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,  String.Empty, cancellationToken);
				}
				else
				{
					byte[] payloadData = receivedDataBuffer.Array.Where(b => b != 0).ToArray();

					//Because we know that is a string, we convert it.
					string receiveString =
					  System.Text.Encoding.UTF8.GetString(payloadData, 0, payloadData.Length);

					//Converts string to byte array.
					var newString =
					  String.Format("Hello, " + receiveString + " ! Time {0}", DateTime.Now.ToString());
					Byte[] bytes = System.Text.Encoding.UTF8.GetBytes(newString);

					//Sends data back.
					await webSocket.SendAsync(new ArraySegment<byte>(bytes),
					  WebSocketMessageType.Text, true, cancellationToken);
				}
			}
		}
	}
}