using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.IO;
using cogbot.Actions.SimExport;
using cogbot.Listeners;
using cogbot.TheOpenSims;
using MushDLR223.Utilities;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.StructuredData;

using MushDLR223.ScriptEngines;

namespace cogbot.Actions.SimExport
{
    public partial class ImportCommand : Command, RegionMasterCommand
    {

        private static readonly object WorkFlowLock = new object();

        public static ImportCommand Running;
        private HashSet<string> arglist;

        private static readonly HashSet<UUID> UnresolvedUUIDs = new HashSet<UUID>();
        static private readonly Dictionary<UUID, UUID> ChangeList = new Dictionary<UUID, UUID>();

        public static readonly Dictionary<UUID, UUIDChange> UUID2OBJECT = new Dictionary<UUID, UUIDChange>();
        public static readonly Dictionary<uint, PrimToCreate> UINT2OBJECT = new Dictionary<uint, PrimToCreate>();

        public static readonly Dictionary<UUID, UUIDChange> NewUUID2OBJECT = new Dictionary<UUID, UUIDChange>();
        public static readonly Dictionary<uint, PrimToCreate> NewUINT2OBJECT = new Dictionary<uint, PrimToCreate>();

        public delegate object ObjectMemberReplacer(MemberInfo name, object before, HashSet<MissingItemInfo> missing);

        internal class ImportSettings
        {
            public UUID GroupID;
            public bool MakeEverythingGroupOwned;
            public HashSet<string> arglist;
            public Simulator CurSim;
        }

        public class UUIDChange
        {
            protected UUIDChange()
            {
                _newId = UUID.Zero;
                _oldId = UUID.Zero;
            }

            private UUID _newId;
            virtual public UUID NewID
            {
                get { return _newId; }
                set { _newId = value; }
            }

            private UUID _oldId;
            virtual public UUID OldID
            {
                get { return _oldId; }
                set { _oldId = value; }
            }
        }

        public static string uuidString(UUID uuid)
        {
            if (CogbotHelpers.IsNullOrZero(uuid)) return "Zero";
            return uuid.ToString();
        }

        static object UUIDReplacer(MemberInfo memberName, object arg, HashSet<MissingItemInfo> missing)
        {
            if (typeof(Primitive) == memberName.DeclaringType)
            {
                string n = memberName.Name;
                if (n == "ID") return arg;
                if (n == "LocalID") return arg;
            }
            if (typeof(Primitive.ObjectProperties) == memberName.DeclaringType)
            {
                string n = memberName.Name;
                if (n == "ObjectID") return arg;
            }
            UUID before = (UUID)arg;
            if (CogbotHelpers.IsNullOrZero(before)) return before;
            if (UnresolvedUUIDs.Contains(before))
            {
                if (missing != null) missing.Add(new MissingItemInfo(memberName, before));
                return before;
            }
            UUID other;
            if (ChangeList.TryGetValue(before, out other))
            {
                return other;
            }
            UUIDChange utc;
            if (UUID2OBJECT.TryGetValue(before, out utc))
            {
                UUID utcNewID = utc.NewID;
                if (!CogbotHelpers.IsNullOrZero(utcNewID)) return utcNewID;
                return utcNewID ?? UUID.Zero;
            }
            if (missing != null) missing.Add(new MissingItemInfo(memberName, before));
            UnresolvedUUIDs.Add(before);
            return before;
        }

        public static UUID GetAssetUploadsFolder()
        {
            UUID assetUploadsFolder = ExportCommand.Running.FolderCalled("AssetUploads");
            return assetUploadsFolder;
        }
        public ImportCommand(BotClient testClient)
        {
            Name = "simimport";
            Description = "Import prims from an exported xml file. Usage: import inputfile.xml [usegroup]";
            Category = CommandCategory.Objects;
            Client.Assets.AssetUploaded += new EventHandler<AssetUploadEventArgs>(Assets_AssetUploaded);
            Client.Objects.ObjectPropertiesFamily += OnObjectPropertiesFamily;
            Client.Network.EventQueueRunning += logged_in;
            ImportPTCFiles(new ImportSettings(), true, false);
            Running = this;
        }

        private void logged_in(object sender, EventQueueRunningEventArgs e)
        {
            Client.Network.EventQueueRunning -= logged_in;
            UUID id = GetAssetUploadsFolder();
            GleanUUIDsFrom(id);
        }


        public override CmdResult Execute(string[] args, UUID fromAgentID, OutputDelegate writeLine)
        {
            ExportCommand.IsExporting = false;
            const string hlp = @"
            
            Toplevel Directives

            // todo  = shows what must be done for import to be complete (suggest adding verbose)
            // perms  = shows what perms are going to be a problem (suggest adding verbose)
            // clear - clear the export dir
            // reset - reset the exporter state
            // cache - blow away asset cache

            // prims [spec] - do only prims meeting spec (default is prims $region) 
            // incr - do only do what is 'todo'
            // nonincr - do things 'todo' but also 'redo' things already done

            // noperms = dont skip things when perms might be a problem
            // quietly = terser output
            // verbose = more verbose
            // request = will rerequest missing things like textures
            // force = will allow unequal LLSD files - this should only be used as last resort

            // users - load users files 
            // prims - load llsd files 
            // trees - load trees files 
            // terrain - load terrain files 
            // links - operate on linkset
            // tasks - save task files
            // taskobjs - task objects
            // all = users llsd tasks deps links (dl and taskobj not included)           
            ";
            if (args == null || args.Length == 0) return Failure(hlp);
            ImportSettings importSettings = new ImportSettings();
            importSettings.GroupID = (args.Length > 1) ? TheBotClient.GroupID : UUID.Zero;
            importSettings.CurSim = Client.Network.CurrentSim;
            arglist = importSettings.arglist = new HashSet<string>();
            writeLine("Starting SimImport...");
            foreach (string s in args)
            {
                arglist.Add(s.TrimEnd(new[] { 's' }).ToLower().TrimStart(new[] { '-' }));
            }
            bool doRez = false;
            if (arglist.Contains("all"))
            {
                //  arglist.Add("terrain");
                arglist.Add("asset");
                arglist.Add("user");
                arglist.Add("group");
                arglist.Add("prim");
                arglist.Add("confirm");                
                arglist.Add("link");
                arglist.Add("task");
                arglist.Add("taskobj");
            }
            if (arglist.Contains("prim"))
            {
                doRez = true;
                arglist.Add("asset");
                arglist.Add("link");
            }
            if (arglist.Contains("nolink"))
            {
                arglist.Remove("link");
            }
            if (arglist.Contains("noasset"))
            {
                arglist.Remove("asset");
            }
            if (arglist.Contains("user") || arglist.Contains("group") || true)
            {
                LoadUsersAndGroups();
            }
            if (arglist.Contains("asset") || true)
            {
                UploadAllAssets(arglist.Contains("sameid"));
            }
            GleanUUIDsFrom(GetAssetUploadsFolder());
            ScanForChangeList();
            if (arglist.Contains("terrain")) UploadTerrain(importSettings);
            WriteLine("NewAsset ChangeList Size is " + ChangeList.Count);

            if (arglist.Contains("confirm")) ImportPTCFiles(importSettings, false, doRez);
            if (arglist.Contains("prim")) ImportPrims(importSettings, doRez);
            if (doRez) RezPrims(importSettings);
            if (arglist.Contains("confirm")) ConfirmLocalIDs(importSettings);
            if (arglist.Contains("link")) ImportLinks(importSettings);
            bool tasksObjs = arglist.Contains("taskobj");
            if (arglist.Contains("task") || tasksObjs) ImportTaskFiles(importSettings, tasksObjs);
            FileStream saveTo = File.Open("MissingFromExport.txt", FileMode.Create);
            var fw = new StreamWriter(saveTo);
            foreach (MissingItemInfo itemInfo in MissingFromExport)
            {
                fw.WriteLine(";; " + itemInfo.MemberName + "\n" + itemInfo.MissingID);
            }
            fw.Close();
            writeLine("Completed SimImport");
            return SuccessOrFailure();
        }

        private UUIDChange GetOld(UUID id)
        {
            lock (WorkFlowLock)
            {
                UUIDChange ptc;
                if (UUID2OBJECT.TryGetValue(id, out ptc))
                {
                    return (UUIDChange)ptc;
                }
            }
            return null;
        }
        private UUID GetChange(UUID id)
        {
            lock (WorkFlowLock)
            {
                UUIDChange ptc;
                if (UUID2OBJECT.TryGetValue(id, out ptc))
                {
                    return ptc.NewID;
                }
            }
            return null;
        }

        private void ScanForChangeList()
        {
            return;
            Dictionary<UUID, UUID> cl = new Dictionary<UUID, UUID>();
            lock (WorkFlowLock)
            {
                foreach (KeyValuePair<UUID, UUIDChange> o in UUID2OBJECT)
                {
                    UUID oValueNewID = o.Value.NewID;
                    if (!(o.Value is UserOrGroupMapping)) ChangeList[o.Key] = oValueNewID;
                }
            }
        }

        private UUIDChange GetNew(UUID id)
        {
            lock (WorkFlowLock)
            {
                UUIDChange ptc;
                if (NewUUID2OBJECT.TryGetValue(id, out ptc))
                {
                    return ptc;
                }
                WriteLine("cant find ID=" + id);
            }
            return null;
        }

        public static object ReplaceAllMembers(object from, Type ofType, ObjectMemberReplacer replacerFunc, HashSet<MissingItemInfo> missing)
        {
            return ReplaceAllMembers(from, ofType, ofType, replacerFunc, new HashSet<object>(), missing);
        }
        public static object ReplaceAllMembers(object from, Type ofType, MemberInfo name, ObjectMemberReplacer replacerFunc, HashSet<object> exceptFor, HashSet<MissingItemInfo> missing)
        {
            if (from == null) return from;
            var fromType = from.GetType();
            if (fromType == ofType)
            {
                var oo = replacerFunc(name, from, missing);
                return oo;
            }
            if (fromType == typeof(string) || typeof(IConvertible).IsAssignableFrom(fromType)) return from;
            if (from is IDictionary)
            {
                var ic = from as IDictionary;
                foreach (var k0 in ic.Keys)
                {
                    var k = k0;
                    var ko = ReplaceAllMembers(k, ofType, k == null ? null : k.GetType(), replacerFunc, exceptFor, missing);                   
                    object o = ic[k];
                    var oo = ReplaceAllMembers(o, ofType, o == null ? null : o.GetType(), replacerFunc, exceptFor, missing);
                    bool keyChanged = false;
                    if (!ReferenceEquals(k, ko))
                    {
                        keyChanged = true;
                        ic.Remove(k);
                        k = ko;
                    }
                    if (ReferenceEquals(oo, o) && !keyChanged) continue;
                    ic[k] = oo;
                }
                return from;
            }
            if (from is IList)
            {
                var ic = from as IList;
                for (int i = 0; i < ic.Count; i++)
                {
                    object o = ic[i];
                    var oo = ReplaceAllMembers(o, ofType, o == null ? null : o.GetType(), replacerFunc, exceptFor, missing);
                    if (ReferenceEquals(oo, o)) continue;
                    ic[i] = oo;
                }
                return from;
            }
            if (exceptFor.Contains(from)) return from;
            exceptFor.Add(from);
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            foreach (var info in fromType.GetFields(bf))
            {
                object o = info.GetValue(from);
                var oo = ReplaceAllMembers(o, ofType, info, replacerFunc, exceptFor, missing);
                if (ReferenceEquals(oo, o)) continue;
                info.SetValue(from, oo);
            }
            foreach (var info in fromType.GetProperties(bf))
            {
                object o = info.GetValue(from, null);
                var oo = ReplaceAllMembers(o, ofType, info, replacerFunc, exceptFor, missing);
                if (ReferenceEquals(oo, o)) continue;
                info.SetValue(from, oo, null);
            }
            return from;
        }

        private void Debug(string s, params object[] ps)
        {
            Client.DisplayNotificationInChat(DLRConsole.SafeFormat(s, ps));
        }
        private void Error(OutputDelegate Failure, string s, params object[] ps)
        {
            string msg = DLRConsole.SafeFormat(s, ps);
            Client.DisplayNotificationInChat(msg);
            Failure(msg);
            return;
            throw new NotImplementedException(msg);
        }

    }
}
