﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using UPnPNet.Gena;
using UPnPNet.Services;

namespace UPnPNet
{
	public class UPnPServer
	{
		public GenaSubscriptionHandler Handler { get; }
		public string Url { get; private set; }
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		public UPnPServer()
		{
			Handler = new GenaSubscriptionHandler();
		}

		public UPnPServer(GenaSubscriptionHandler handler)
		{
			Handler = handler;
		}

		public void Start(IPEndPoint endpoint)
		{
			Url = $"http://{endpoint.Address}:{endpoint.Port}/notify";
			

			var host = new WebHostBuilder()
				.UseKestrel()
				.UseUrls($"http://{endpoint.Address}:{endpoint.Port}")
				.Configure(app =>
				{
					app.Run(handler =>
					{
						StreamReader reader = new StreamReader(handler.Request.Body);
						IDictionary<string, string> headers =
							handler.Request.Headers.ToDictionary(
								x => x.Key,
								x => x.Value.Aggregate((prev, next) => prev + " " + next));

						Handler.HandleNotify(headers, reader.ReadToEnd());

						handler.Response.StatusCode = 200;
						return Task.FromResult(0);
					});
				})
				.Build();

			Task.Run(() => host.Run(_cancellationTokenSource.Token));
		}

		public void Stop()
		{
			_cancellationTokenSource.Cancel();
		}

		public async void SubscribeToControl<T>(UPnPLastChangeServiceControl<T> control)
		{
			await control.SubscribeToEvents(Handler, Url, 3600);
		}
	}
}