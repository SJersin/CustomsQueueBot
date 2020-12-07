using System;
using System.Collections.Generic;
using System.Text;

namespace CustomsQueueBot
{
    class Program
    {
        static void Main(string[] args)
            => new Bot().MainAsync().GetAwaiter().GetResult();
    }
}
