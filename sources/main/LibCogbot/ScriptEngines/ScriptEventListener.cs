using System;
using System.Collections.Generic;
using System.Text;
using Cogbot;
using System.Threading;
using System.IO;
using System.Collections;
using MushDLR223.ScriptEngines;
using MushDLR223.Utilities;
using OpenMetaverse;
using System.Reflection;
using Cogbot.World;
using OpenMetaverse.Assets;
#if (COGBOT_LIBOMV || USE_STHREADS)
using ThreadPoolUtil;
using Thread = ThreadPoolUtil.Thread;
using ThreadPool = ThreadPoolUtil.ThreadPool;
using Monitor = ThreadPoolUtil.Monitor;
#endif
using System.Threading;



namespace Cogbot.ScriptEngines
{
    public class ScriptEventListener : SimEventSubscriber, IDisposable
    {
        readonly Queue<KeyValuePair<object, CogbotEvent>> taskQueue = new Queue<KeyValuePair<object, CogbotEvent>>();
        private ScriptInterpreter taskInterperter;
        readonly Thread thrJobQueue;
        readonly WorldObjects WorldSystem;
        public ScriptEventListener(ScriptInterpreter interp, BotClient client)
        {
            taskInterperter = interp;
            taskInterperter.Intern("Client", client);
            taskInterperter.Intern("thisClient", client);
            if (client != null) WorldSystem = client.WorldSystem;

            thrJobQueue = new Thread(jobManager);
            thrJobQueue.Name = string.Format("ScriptEventListener Thread for {0}", (client ?? (Object)"ClientManager"));
            thrJobQueue.Start();
        }

        public void jobManager()
        {
            while (taskInterperter != null)
            {
                while (taskQueue.Count > 0)
                {
                    try
                    {
                        taskTick();
                    }
                    catch (Exception e)
                    {
                        DLRConsole.DebugWriteLine("!Exception: " + e.GetBaseException().Message);
                        DLRConsole.DebugWriteLine("error occured: " + e.Message);
                        DLRConsole.DebugWriteLine("        Stack: " + e.StackTrace.ToString());
                    }
                    Thread.Sleep(5);
                }
                Thread.Sleep(500);
            }
        }


        public Object genLispCodeTree(string lispCode)
        {
            Object codeTree = null;
            try
            {
                StringReader stringCodeReader = new System.IO.StringReader(lispCode);
                codeTree = taskInterperter.Read("enqueueLispEvent", stringCodeReader, DLRConsole.DebugWriteLine);
                if (taskInterperter.Eof(codeTree))
                    return null;
            }
            catch (Exception e)
            {
                DLRConsole.DebugWriteLine(lispCode + " -> " + e);
                return null;
            }
            return codeTree;
        }

        internal void enqueueLispTask(object lispObject)
        {
            lock (taskQueue)
            {
                taskQueue.Enqueue(taskFromCodeTree(lispObject));
            }
        }

        public void enqueueLispEvent(string lispCode)
        {
            DLRConsole.DebugWriteLine(":: " + lispCode);
            try
            {
                enqueueLispTask(taskInterperter.Read("enqueueLispEvent", new StringReader(lispCode), DLRConsole.DebugWriteLine));
            }
            catch (Exception e)
            {
                DLRConsole.DebugWriteLine("!Exception: " + e.GetBaseException().Message);
                DLRConsole.DebugWriteLine("error occured: " + e.Message);
                DLRConsole.DebugWriteLine("        Stack: " + e.StackTrace.ToString());
                DLRConsole.DebugWriteLine("     LispCode: " + lispCode);
            }
        }

        private KeyValuePair<object, CogbotEvent> taskFromCodeTree(object lispObject)
        {
            CogbotEvent evt = new ACogbotEvent(taskInterperter, SimEventType.SCRIPT | SimEventType.PERSONAL, "enqueue", new[] { lispObject });
            return new KeyValuePair<object, CogbotEvent>(lispObject, evt);
        }


        static public string argsListString(IEnumerable args)
        {
            if (args == null) return "NiL";
            IEnumerator enumer = args.GetEnumerator();
            if (enumer == null) return "niL";
            if (!enumer.MoveNext()) return "";
            String msg = argString(enumer.Current);
            while (enumer.MoveNext())
            {
                msg += " ";
                msg += argString(enumer.Current);
            }
            return msg;

        }

        static public string argString(object arg)
        {
            if (arg == null) return "NIL";
            Type type = arg.GetType();
            if (arg is BotClient)
            {
                arg = ((BotClient)arg).GetAvatar();
                if (arg is BotClient)
                {
                    return ((BotClient)arg).ToString();
                }
            }
            if (arg is WorldObjects)
            {
                arg = ((WorldObjects)arg).TheSimAvatar;
            }
            if (arg is Simulator)
            {
                Simulator sim = (Simulator)arg;
                uint globalX, globalY;
                Utils.LongToUInts(sim.Handle, out globalX, out globalY);
                return "'(simulator " + argString(sim.Name) + " " + globalX / 256 + " " + globalY / 256 + " " +
                       argString(sim.IPEndPoint.ToString()) + ")";
            }
            if (arg is Avatar)
            {
                Avatar prim = (Avatar)arg;
                arg = "'(SimAvatarFn "; //+ argString(prim.ID.ToString());
                if (prim.Name != null)
                {
                    arg = arg + " " + argString(prim.Name);
                }
                return arg + ")";
            }

            if ((arg is AssetAnimation) || (arg is AssetTexture) || (arg is AssetSound))
            {
                Asset prim = (Asset)arg;
                arg = SimAssetStore.GetSimAsset(prim);
            }

            if (arg is SimAsset)
            {
                SimAsset prim = (SimAsset)arg;
                AssetType tyepe = prim.AssetType;
                arg = "'(Sim" + tyepe + "Fn "; //+ argString(prim.ID.ToString());
                if (prim.Name != null)
                {
                    arg = arg + " " + argString(prim.Name);
                }
                return arg + ")";
            }

            if (arg is Primitive)
            {
                Primitive prim = (Primitive)arg;
                arg = "'(SimObjectFn  " + argString(prim.ID.ToString());
                if (prim.Properties != null)
                {
                    arg = arg + " " + argString(prim.Properties.Name);
                }
                return arg + ")";
            }
            if (arg is SimAvatar)
            {
                SimAvatar prim = (SimAvatar)arg;
                arg = "'(SimAvatarFn  " + argString(prim.GetName());
                return arg + ")";
            }
            if (arg is SimObject)
            {
                SimObject prim = (SimObject)arg;
                arg = "'(SimObjectFn  " + argString(prim.ID.ToString());
                string name = prim.GetName();
                if (!string.IsNullOrEmpty(name))
                {
                    arg = arg + " #|" + argString(name) + "|# ";
                }
                return arg + ")";
            }
            if (type.IsEnum)
            {
                return argString(arg.ToString());
            }
            //InternalDictionary
            if (arg is IList)
            {
                String dictname = "'(list " + type.Name;
                IList list = (IList)arg;
                foreach (object key in list)
                {
                    dictname += " " + argString(key);
                }
                return dictname + ")";


            }

            if (arg is Parcel)
            {
                String dictname = "'(parcel";
                Parcel list = (Parcel)arg;
                dictname += " " + argString(list.SnapshotID.ToString());
                dictname += " " + argString(list.Name);
                return dictname + ")";
            }
            if (arg is Group)
            {
                String dictname = "'(Group";
                Group list = (Group)arg;
                dictname += " " + argString(list.Name);
                return dictname + ")";
            }
            if (arg is IDictionary)
            {
                String dictname = "'(dict " + type.Name;
                IDictionary dict0 = (IDictionary)arg;
                IDictionary dict = dict0;
                lock (dict.SyncRoot)
                {
                    foreach (object key in dict.Keys)
                    {
                        Object o = dict[key];
                        dictname += " " + argString(key) + "=" + argString(o);
                    }
                    return dictname + ")";
                }

            }

            //if (arg is Quaternion)
            //{
            //    Quaternion quat = (Quaternion)arg;
            //    quat.Normalize();
            //    arg = WorldSystem.QuatToRotation(quat);
            //}

            if (arg is Quaternion)
            {
                Quaternion vect = (Quaternion)arg;
                return "'(Quaternion " + vect.X + " " + vect.Y + " " + vect.Z + " " + vect.W + ")";
            }

            if (arg is UUID)
            {
                //   if (true) return argString(arg.ToString());
                object found = WorldObjects.GridMaster.GetObject((UUID)arg);
                if (found == null || found is UUID)
                {
                    return argString(arg.ToString());
                }
                return argString(found);
            }

            if (arg is Vector3)
            {
                Vector3 vect = (Vector3)arg;
                return "'(Vector3 " + vect.X + " " + vect.Y + " " + vect.Z + ")";
            }

            if (arg is Vector2)
            {
                Vector2 vect = (Vector2)arg;
                return "'(Vector2 " + vect.X + " " + vect.Y + ")";
            }

            if (arg is Vector3d)
            {
                Vector3d vect = (Vector3d)arg;
                return "'(Vector3d " + vect.X + " " + vect.Y + " " + vect.Z + ")";
            }

            if (type.IsArray)
            {
                Array a = (Array)arg;
                return "#{/*" + type + "*/" + argsListString(a) + "}";
            }
            if (arg is String)
            {
                return "\"" + arg.ToString().Replace("\"", "\\\"") + "\"";
            }
            if (type.Namespace.StartsWith("System"))
            {
                return "" + arg;
            }
            if (arg is IEnumerable)
            {
                IEnumerable a = (IEnumerable)arg;
                return "'(/*" + type + "*/" + argsListString(a) + ")";
            }
            if (type.IsValueType)
            {
                String tostr = "{" + arg + "";
                foreach (FieldInfo fi in type.GetFields())
                {
                    if (!fi.IsStatic)
                    {
                        tostr += ",";
                        tostr += fi.Name + "=";
                        tostr += argString(fi.GetValue(arg));
                    }
                }
                return argString(tostr + "}");
            }
            if (!type.IsValueType)
            {
                String tostr = "{" + arg + "";
                foreach (FieldInfo fi in type.GetFields())
                {
                    if (!fi.IsStatic)
                    {
                        tostr += ",";
                        tostr += fi.Name + "=";
                        tostr += fi.GetValue(arg);
                    }
                }
                return argString(tostr + "}");
            }
            return "" + arg;
        }

        public void taskTick()
        {
            if (taskInterperter == null)
            {
                // Abort self
                Thread.CurrentThread.Abort();
                return;
            }
            //   string lastcode = "";
            string codeString = null;
            try
            {
                // see if there is anything to process
                if (taskQueue.Count == 0) return;
                KeyValuePair<object, CogbotEvent> thisTask;

                // if so then process it
                //Interpreter lispInterperter = new Interpreter();
                lock (taskQueue)
                {
                    thisTask = taskQueue.Dequeue();
                }
                // setup the local context
                //lastcode = thisTask.code;
                //string serverMessage = "";
                //  thisTask.results = "'(unevaluated)";
                //taskInterperter.Intern("thisTask", thisTask);
                //should make the following safer ...
                //taskInterperter.Intern("tcpReader", tcpStreamReader);
                //taskInterperter.Intern("tcpWriter", tcpStreamWriter);
                //a safer way is to have a serverMessage string that is sent to the Client
                // in a more thread safe async way
                //taskInterperter.Intern("serverMessage", serverMessage);
                //taskInterperter.Intern("Client",Command.Client);

                // EVALUATE !!!
                codeString = taskInterperter.Str(thisTask.Key);
                Object x = taskInterperter.Eval(thisTask.Key);
                // thisTask.results = taskInterperter.Str(x);
                //lock (lBotMsgSubscribers)
                //{
                //    foreach (BotMessageSubscriber ms in lBotMsgSubscribers)
                //    {
                //        if (ms is Utilities.BotTcpServer)
                //        {
                //            //((Utilities.BotTcpServer)ms).taskTick(thisTask.results);
                //        }
                //    }
                //}
                if (false)
                {
                    // WriteLine(" taskcode: " + lastcode + " --> " + thisTask.results);
                    //WriteLine(" taskTick Results>" + thisTask.results);
                    //WriteLine(" taskTick continueTask=" + thisTask.requeue.ToString());
                }

                // Should we do again ?
                //if (thisTask.requeue == true)
                //{
                //    if (!lastcode.Equals(thisTask.code))
                //    {
                //        // not the same so must "re-compile"
                //        thisTask.codeTree = genLispCodeTree(thisTask.code);
                //    }
                //    lock (taskQueue)
                //    {
                //        taskQueue.Enqueue(thisTask);
                //    }
                //}
                return;
            }
            catch (Exception e)
            {
                DLRConsole.DebugWriteLine("!Exception: " + e.GetBaseException().Message);
                DLRConsole.DebugWriteLine("error occured: " + e.Message);
                DLRConsole.DebugWriteLine("        Stack: " + e.StackTrace.ToString());
                DLRConsole.DebugWriteLine("     LispCode: " + codeString);
            }
        }


        #region SimEventSubscriber Members

        void SimEventSubscriber.OnEvent(CogbotEvent evt)
        {
            if (taskInterperter != null && taskInterperter.IsSubscriberOf(evt.Verb))
            {
                object lispCode = lispCodeFromEvent(evt);
                taskQueue.Enqueue(new KeyValuePair<object, CogbotEvent>(lispCode, evt));
            }
        }

        private object lispCodeFromEvent(CogbotEvent evt)
        {
            return genLispCodeTree("(" + evt.Verb.ToLower() + " " + argsListString(evt.GetArgs()) + ")");
        }

        void SimEventSubscriber.Dispose()
        {
            ((ScriptEventListener) this).Dispose();
        }

        public bool EventsEnabled
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        #endregion

        public void Dispose()
        {
            thrJobQueue.Abort();
            lock (taskQueue)
            {
                taskQueue.Clear();
            }
            //TODO decide if we should do this here
            //  taskInterperter.Dispose();
            taskInterperter = null;
        }
    }
}
