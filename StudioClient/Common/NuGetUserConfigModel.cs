using System.Collections.Generic;

namespace StudioClient.Common
{
    /// <summary>
    /// NuGet 用户配置文件模型 - Config/NuGetUser.Config.yml
    /// </summary>
    class NuGetUserConfigModel
    {
        public Dictionary<string, string> PackageSources { get; set; }
    }
}
