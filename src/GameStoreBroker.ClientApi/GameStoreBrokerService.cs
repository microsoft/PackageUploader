// Copyright (C) Microsoft. All rights reserved.

using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Xfus;
using Microsoft.Extensions.Logging;

namespace GameStoreBroker.ClientApi
{
	public class GameStoreBrokerService
	{
		private readonly ILogger<GameStoreBrokerService> _logger;
		private readonly IngestionHttpClient _ingestionHttpClient;
		private readonly XfusHttpClient _xfusHttpClient;

		public GameStoreBrokerService(ILogger<GameStoreBrokerService> logger, IngestionHttpClient ingestionHttpClient, XfusHttpClient xfusHttpClient)
		{
			_logger = logger;
			_ingestionHttpClient = ingestionHttpClient;
			_xfusHttpClient = xfusHttpClient;
		}

		public async Task<bool> UploadGame()
		{
			await Task.Delay(0);
			_logger.LogInformation("Uploading...");
			return true;
		}

	}
}