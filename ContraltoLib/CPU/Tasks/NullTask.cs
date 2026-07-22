

namespace Contralto.CPU
{
    public partial class AltoCPU
    {
        private sealed class NullTask : Task
        {
            public NullTask(AltoCPU cpu) : base(cpu)
            {
            }
        }
    }
}
