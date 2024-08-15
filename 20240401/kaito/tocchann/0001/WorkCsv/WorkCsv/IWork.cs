using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkCsv;

enum Command
{
	None,
	Sort,
	Swap,
	Sum,
}
internal interface IWork
{
	void Execute( CsvParser csv, string[] args );
}
