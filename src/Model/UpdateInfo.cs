using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.Model
{
    internal class UpdateInfo
    {
        /// <summary>
        /// 日志
        /// </summary>
        public string? Log { get; set; }

        public UpdateInfoType Type { get; set; }

        public Awe.Model.OpenFrp.Response.Data.SoftWareVersionData? SoftWareVersionData { get; set; }
    }
    internal enum UpdateInfoType
    {
        None,
        Launcher,
        FrpClient
    }
}
