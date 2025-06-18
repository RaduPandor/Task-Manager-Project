using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager.Services
{
    public interface IProcessProvider
    {
        Process[] GetProcesses();
    }
}
