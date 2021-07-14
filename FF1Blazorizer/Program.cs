using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using BlazorStrap;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Blazored.LocalStorage;
using FF1Blazorizer;

namespace FF1Blazorizer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
			builder.Services.AddBootstrapCss();
			builder.Services.AddBlazoredLocalStorage();
			builder.Services.AddScoped<RandomizerMode>();

            await builder.Build().RunAsync();
        }
	}

	public class RandomizerMode
	{
		public bool AdvancedEnabled { get; set; }
		public event Action OnChange;

		public void SetMode(bool enableadvanced)
		{
			AdvancedEnabled = enableadvanced;
			NotifyStateChanged();
		}
		public void Toggle()
		{
			AdvancedEnabled = !AdvancedEnabled;
			NotifyStateChanged();
		}
		private void NotifyStateChanged()
		{
			OnChange?.Invoke();
		}
	}
}
