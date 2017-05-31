using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Sitecore.Diagnostics;
using Sitecore.Install.Framework;
using Sitecore.Pipelines;
using Sitecore.Support.Update.Installer.Items;
using Sitecore.Update.Installer.Items;

namespace Sitecore.Support
{
  public class InitializeSinkItemFactory
  {
    public void Process(PipelineArgs args)
    {
      var installers = GetInstallers();
      installers["addeditems_Upgrade"] = new Sitecore.Support.Update.Installer.Items.AddItemCommandInstaller();
    }

    private Dictionary<string, ISink<PackageEntry>> GetInstallers()
    {
      return
        typeof(SinkItemFactory).GetField("installers", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as
          Dictionary<string, ISink<PackageEntry>>;
    }
  }
}