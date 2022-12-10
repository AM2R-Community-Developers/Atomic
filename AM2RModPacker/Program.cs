using System;
using Eto.Forms;

namespace AM2RModPacker;

class Program
{
	[STAThread]
	static void Main(string[] args)
	{
		new Application(Eto.Platform.Detect).Run(new ModPacker());
	}
}