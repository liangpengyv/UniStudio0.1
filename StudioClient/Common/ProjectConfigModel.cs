using System.Collections.Generic;

namespace StudioClient.Common
{
    /// <summary>
    /// 项目配置文件模型 - project.yml
    /// </summary>
    public class ProjectConfigModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Main { get; set; }
        public Dictionary<string, string> Dependencies { get; set; }
    }
}
