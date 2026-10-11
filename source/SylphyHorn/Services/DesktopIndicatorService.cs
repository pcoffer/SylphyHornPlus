using System;
using SylphyHorn.Services.DesktopTransitions;

namespace SylphyHorn.Services
{
	internal sealed class DesktopIndicatorService : IDisposable
	{
		private readonly ITaskbarIndicatorHost _host;
		private DesktopTransitionRuntime _runtime;

		internal DesktopIndicatorService(ITaskbarIndicatorHost host)
		{
			this._host = host ?? throw new ArgumentNullException(nameof(host));
		}

		internal void BindDesktopRuntime(DesktopTransitionRuntime runtime)
		{
			if (runtime == null) throw new ArgumentNullException(nameof(runtime));
			if (this._runtime != null)
			{
				if (ReferenceEquals(this._runtime, runtime)) return;
				throw new InvalidOperationException("DesktopIndicatorService cannot be rebound to another desktop runtime.");
			}
			this._runtime = runtime;
			runtime.StateChanged += this.OnDesktopStateChanged;
			this.Update(runtime.State);
		}

		internal static string CreateText(DesktopRuntimeState state)
		{
			if (state?.CurrentDesktopId == null || !state.Records.TryGetValue(state.CurrentDesktopId.Value, out var record)) return null;
			var number = state.Order.IndexOf(state.CurrentDesktopId.Value) + 1;
			if (number <= 0) return null;
			return DesktopIndicatorTextFormatter.Create(number, record.Name.HasValue ? record.Name.Value : null);
		}

		private void OnDesktopStateChanged(object sender, DesktopRuntimeStateChanged e) => this.Update(e.Change.Snapshot);

		private void Update(DesktopRuntimeState state)
		{
			var text = CreateText(state);
			if (text != null) this._host.SetText(text);
		}

		public void Dispose()
		{
			if (this._runtime != null) this._runtime.StateChanged -= this.OnDesktopStateChanged;
			this._runtime = null;
			this._host.Dispose();
		}
	}
}
