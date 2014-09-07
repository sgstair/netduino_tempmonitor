using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataReceiverService
{
       
    [Database]
    class TempMonitorSql : DataContext
    {
        public TempMonitorSql()
            : base("Server=nucsql;Database=TempMonitor;User=temperature_monitor;Password=2368e4dc-bc0c-486e-9e52-1d157b83ad54;Pooling=true")
        {

        }

        public Table<TemperatureData> TemperatureData
        {
            get
            {
                return this.GetTable<TemperatureData>();
            }
        }
    }

    [Table]
    class TemperatureData
    {
        [Column(IsPrimaryKey=true)]
        public DateTime Time;

        [Column(IsPrimaryKey = true)]
        public byte[] Device;

        [Column]
        public double? Value;
    }
}
