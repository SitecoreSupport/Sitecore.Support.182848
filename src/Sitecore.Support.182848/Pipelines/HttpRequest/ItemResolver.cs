namespace Sitecore.Support.Pipelines.HttpRequest
{
    using Sitecore;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.ItemResolvers;
    using Sitecore.Data.Items;
    using Sitecore.Data.Managers;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.IO;
    using Sitecore.Pipelines.HttpRequest;
    using Sitecore.SecurityModel;
    using Sitecore.Sites;
    using System;
    using System.Linq;

    public class ItemResolver : HttpRequestProcessor
    {
        #region Original code

        public override void Process(HttpRequestArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (Context.Item != null || Context.Database == null || args.Url.ItemPath.Length == 0)
            {
                return;
            }

            Profiler.StartOperation("Resolve current item.");
            string path = MainUtil.DecodeName(args.Url.ItemPath);
            Item result = args.GetItem(path);

            if (result == null)
            {
                path = args.Url.ItemPath;
                result = args.GetItem(path);
            }

            if (result == null)
            {
                path = args.LocalPath;
                result = args.GetItem(path);
            }
            if (result == null)
            {
                path = MainUtil.DecodeName(args.LocalPath);
                result = args.GetItem(path);
            }

            SiteContext site = Context.Site;
            string siteRootPath = site != null ? site.RootPath : string.Empty;

            if (result == null)
            {
                path = FileUtil.MakePath(siteRootPath, args.LocalPath, '/');
                result = args.GetItem(path);
            }

            if (result == null)
            {
                path = MainUtil.DecodeName(FileUtil.MakePath(siteRootPath, args.LocalPath, '/'));
                result = args.GetItem(path);
            }

            if (result == null)
            {
                result = ResolveUsingDisplayName(args);
            }
            if (result == null && args.UseSiteStartPath && site != null)
            {
                result = args.GetItem(site.StartPath);
            }

            if (result != null)
            {
                Tracer.Info("Current item is \"" + path + "\".");
            }

            Context.Item = result;
            Profiler.EndOperation();
        }

        #endregion

        #region Added code
       
        private Item GetChild(Item item, string itemName)
        {
            foreach (Item obj in item.Children)
            {
                if (MainUtil.EncodeName(obj.DisplayName).Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
                    MainUtil.EncodeName(obj.Name).Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
                    obj.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
                    obj.DisplayName.Equals(itemName, StringComparison.OrdinalIgnoreCase))
                {
                    return obj;
                }
            }
            return null;
        }

        private Item GetSubItem(string path, Item root)
        {
            Item obj = root;
            string str = path;
            char[] chArray = { '/' };
            foreach (string itemName in str.Split(chArray))
            {
                if (itemName.Length != 0)
                {
                    obj = GetChild(obj, itemName);
                    if (obj == null)
                    {
                        return null;
                    }
                }
            }
            return obj;
        }

        #endregion

        #region Modified code

        private Item ResolveFullPath(HttpRequestArgs args)
        {
            string itemPath = args.Url.ItemPath;
            if (string.IsNullOrEmpty(itemPath) || itemPath[0] != 47)
            {
                return null;
            }
            int num = itemPath.IndexOf('/', 1);
            if (num < 0)
            {
                return null;
            }
            Item root = ItemManager.GetItem(itemPath.Substring(0, num), Sitecore.Globalization.Language.Current, 
                Sitecore.Data.Version.Latest, Context.Database, SecurityCheck.Disable);
            return root == null ? null : GetSubItem(itemPath.Substring(num), root);   //if root is null call GetSubItem method
        }

        private Item ResolveLocalPath(HttpRequestArgs args)
        {
            SiteContext site = Context.Site;
            if (site == null) return null;
            Item root = ItemManager.GetItem(site.RootPath, Language.Current, Sitecore.Data.Version.Latest, 
                Context.Database, SecurityCheck.Disable);
            return root == null ? null : GetSubItem(args.LocalPath, root); //if root is null call GetSubItem method
        }

        #endregion

        #region Original code

        private Item ResolveUsingDisplayName(HttpRequestArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Item obj;
            using (new SecurityDisabler())
            {
                obj = ResolveLocalPath(args) ?? ResolveFullPath(args);
                if (obj == null)
                {
                    return null;
                }
            }
            return args.ApplySecurity(obj);
        }

        #endregion
    }
}
