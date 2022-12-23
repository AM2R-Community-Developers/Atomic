using Eto.Forms;

namespace Atomic.Mac;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        new Application(Eto.Platforms.Mac64).Run(new ModPacker());
    }
}