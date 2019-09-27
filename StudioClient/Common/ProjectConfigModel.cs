using System.Collections.Generic;

namespace StudioClient.Common
{
    /// <summary>
    /// 项目配置文件 - project.yml 模型
    /// </summary>
    public class ProjectConfigModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Main { get; set; }
        public Dictionary<string, string> Dependencies { get; set; }

        public ProjectConfigModel() { }

        public ProjectConfigModel(string name, string description, string main, Dictionary<string, string> dependencies)
        {
            this.Name = name;
            this.Description = description;
            this.Main = main;
            this.Dependencies = dependencies;
        }
    }
}
