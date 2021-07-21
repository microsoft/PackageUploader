// Copyright (C) Microsoft. All rights reserved.

using GameStoreBroker.ClientApi.Xfus;
using Microsoft.Extensions.DependencyInjection;

namespace GameStoreBroker.ClientApi
{
	public static class GameStoreBrokerExtensions
	{
		public static void AddGameStoreBrokerService(this IServiceCollection services)
		{
			services.AddHttpClient<IngestionHttpClient>();
			services.AddHttpClient<XfusHttpClient>();
			services.AddScoped<GameStoreBrokerService>();
		}
	}
}