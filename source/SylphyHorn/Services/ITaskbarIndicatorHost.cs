using System;

namespace SylphyHorn.Services
{
	// Keeps the Windows shell hosting mechanism out of the desktop indicator logic.
	internal interface ITaskbarIndicatorHost : IDisposable
	{
		void SetText(string text);
	}
}
