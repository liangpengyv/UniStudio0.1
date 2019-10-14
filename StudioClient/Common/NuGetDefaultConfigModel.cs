using System.Collections.Generic;

namespace StudioClient.Common
{
    /// <summary>
    /// NuGet 默认配置文件模型 - Config/NuGetDefault.Config.yml
    /// </summary>
    class NuGetDefaultConfigModel
    {
        public Dictionary<string, string> PackageSources { get; set; }
    }
}
