﻿using Couchbase.Lite.DI;
using Couchbase.Lite.Sync;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Couchbase.Lite;
using LiteCore.Interop;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Runtime.CompilerServices;
using System.Net.NetworkInformation;

namespace Test
{
    public unsafe interface IWebSocketWrapper : IDisposable
    {
        Stream NetworkStream { get; set; }
        unsafe void CloseSocket();
        void CompletedReceive(ulong byteCount);
        void Start();
        void Write(byte[] data);
    }

#if COUCHBASE_ENTERPRISE


#endif
    public sealed unsafe class WebSocketTest : TestCase
    {
        Type WebSocketWrapperType = typeof(WebSocketWrapper);
        Type ReachabilityType = typeof(Reachability);
        Type HttpLogicType = typeof(HTTPLogic);
        WebSocketWrapper webSocketWrapper;
        HTTPLogic hTTPLogic = new HTTPLogic(new Uri("ws://localhost:4984"));

#if !WINDOWS_UWP
        public WebSocketTest(ITestOutputHelper output) : base(output)
        {
            var dict = new ReplicatorOptionsDictionary();
            var authDict = new AuthOptionsDictionary();
            authDict.Username = "admin";
            authDict.Password = "admintest";
            authDict.Type = AuthType.HttpBasic;
            dict.Auth = authDict;
            dict.Headers.Add("User-Agent", "CouchbaseLite/2.1.0 (.NET; Microsoft Windows 10.0.17134 ) Build/0 LiteCore/ (1261) Commit/b01fad7");
            dict.Add("WS-Protocols", "BLIP_3+CBMobile_2");
            var uri = new Uri("ws://localhost:4984");

            var s = new LiteCore.Interop.C4Socket();
            webSocketWrapper = new WebSocketWrapper(uri, (&s), dict);
        }
#endif

        [Fact]
        public void TestWebSocketWrapper()
        {
            var method = WebSocketWrapperType.GetMethod("Start", BindingFlags.Public | BindingFlags.Instance);
            //method.Invoke(webSocketWrapper, null);

            byte[] byteArray = Encoding.ASCII.GetBytes("websocket testing");

            method = WebSocketWrapperType.GetMethod("Write", BindingFlags.Public | BindingFlags.Instance);
            method.Invoke(webSocketWrapper, new object[1] { byteArray });

            method = WebSocketWrapperType.GetMethod("CompletedReceive", BindingFlags.Public | BindingFlags.Instance);
            method.Invoke(webSocketWrapper, new object[1] { (ulong)byteArray.Length });

            method = WebSocketWrapperType.GetMethod("CloseSocket", BindingFlags.Public | BindingFlags.Instance);
            //method.Invoke(webSocketWrapper, null);

            method = WebSocketWrapperType.GetMethod("Receive", BindingFlags.NonPublic | BindingFlags.Instance);
            //method.Invoke(webSocketWrapper, new object[1] { byteArray });

            method = WebSocketWrapperType.GetMethod("StartInternal", BindingFlags.NonPublic | BindingFlags.Instance);
            //method.Invoke(webSocketWrapper, null);
        }

        [Fact]
        public void TestNetworkTaskSuccessful()
        {
            var method = WebSocketWrapperType.GetMethod("NetworkTaskSuccessful", BindingFlags.NonPublic | BindingFlags.Instance);
            //var res = method.Invoke(webSocketWrapper, new object[1] { t });
        }

        [Fact]
        public void TestDidClose()
        {
            C4WebSocketCloseCode closeCode = C4WebSocketCloseCode.WebSocketCloseAbnormal;
            string reason = "Unexpected error in client logic";
            var method = webSocketWrapper.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(m => m.Name == "DidClose");
            //var res = method.ElementAt(0).Invoke(webSocketWrapper, new object[2] { closeCode, reason });
        }

        [Fact]
        public void TestBase64Digest()
        {
            string input = "test coverage";

            var method = WebSocketWrapperType.GetMethod("Base64Digest", BindingFlags.NonPublic | BindingFlags.Static);
            var res = method.Invoke(null, new object[1] { input });

            res.Should().Be("/EOfaNlb2IwudhJuOmFE3Ps9D38=");
        }

        [Fact]
        public void TestHandleHTTPResponse()
        {
            var method = WebSocketWrapperType.GetMethod("HandleHTTPResponse", BindingFlags.NonPublic | BindingFlags.Instance);
            //var res = method.Invoke(webSocketWrapper, null);
        }

        [Fact]
        public void TestCheckHeader()
        {
            HttpMessageParser parser = new HttpMessageParser("HTTP/1.1 401 Unauthorized");
            parser.Append("Content - Type: application / json");
            parser.Append("Server: Couchbase Sync Gateway/2.0.0");
            parser.Append("Www-Authenticate: Basic realm=\"Couchbase Sync Gateway\"");
            parser.Append("Date: Mon, 06 Aug 2018 17:44:51 GMT");
            parser.Append("Content-Length: 50");
            string key = "Connection";
            string expectedValue = "Upgrade";
            bool caseSens = false; // true

            var method = WebSocketWrapperType.GetMethod("CheckHeader", BindingFlags.NonPublic | BindingFlags.Static);
            var res = method.Invoke(null, new object[4] { parser, key, expectedValue, caseSens });

            res.Should().Be(false);

            parser = new HttpMessageParser("HTTP/1.1 101 Switching Protocols");
            parser.Append("Upgrade: websocket");
            parser.Append("Connection: Upgrade");
            parser.Append("Sec-WebSocket-Accept: R3ztu/aZLI+izEEtS3Ao1kzub4s=");
            parser.Append("Sec-WebSocket-Protocol: BLIP_3+CBMobile_2");

            key = "Connection";
            expectedValue = "Upgrade";
            caseSens = false;

            method = WebSocketWrapperType.GetMethod("CheckHeader", BindingFlags.NonPublic | BindingFlags.Static);
            res = method.Invoke(null, new object[4] { parser, key, expectedValue, caseSens });

            res.Should().Be(true);

            key = "Upgrade";
            expectedValue = "websocket";
            caseSens = false;

            method = WebSocketWrapperType.GetMethod("CheckHeader", BindingFlags.NonPublic | BindingFlags.Static);
            res = method.Invoke(null, new object[4] { parser, key, expectedValue, caseSens });

            res.Should().Be(true);

            key = "Sec-WebSocket-Accept";
            expectedValue = "R3ztu/aZLI+izEEtS3Ao1kzub4s=";
            caseSens = true;

            method = WebSocketWrapperType.GetMethod("CheckHeader", BindingFlags.NonPublic | BindingFlags.Static);
            res = method.Invoke(null, new object[4] { parser, key, expectedValue, caseSens });

            res.Should().Be(true);

            method = WebSocketWrapperType.GetMethod("Connected", BindingFlags.NonPublic | BindingFlags.Instance);
            //res = method.Invoke(webSocketWrapper, null);
            
            method = WebSocketWrapperType.GetMethod("ReceivedHttpResponse", BindingFlags.NonPublic | BindingFlags.Instance);
            //res = method.Invoke(webSocketWrapper, new object[1] { parser });

            method = HttpLogicType.GetMethod("ReceivedResponse", BindingFlags.Public | BindingFlags.Instance);
            res = method.Invoke(hTTPLogic, new object[1] { parser });

            method = HttpLogicType.GetMethod("Redirect", BindingFlags.NonPublic | BindingFlags.Instance);
            res = method.Invoke(hTTPLogic, new object[1] { parser });
        }

        [Fact]
        public unsafe void TestPerformWrite()
        {
            var method = WebSocketWrapperType.GetMethod("PerformWrite", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(webSocketWrapper, null);
        }

        [Fact]
        public unsafe void TestPerformRead()
        {
            var method = WebSocketWrapperType.GetMethod("PerformRead", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(webSocketWrapper, null);
        }

        [Fact]
        public void TestOnSocketReady()
        {
            var method = WebSocketWrapperType.GetMethod("OnSocketReady", BindingFlags.NonPublic | BindingFlags.Instance);
            //var res = method.Invoke(wsw, null);
        }

        [Fact]
        public void TestReachability()
        {
            var status = NetworkReachabilityStatus.Unknown;
            var e = new NetworkReachabilityChangeEventArgs(status);
            NetworkReachabilityStatus s = NetworkReachabilityStatus.Unknown;

            var _reachability = new Reachability();

            _reachability.StatusChanged += (sender, args) => s = args.Status;
            _reachability.Start();

            e.Status.Should().Be(s);

            var method = ReachabilityType.GetMethod("InvokeNetworkChangeEvent", BindingFlags.NonPublic | BindingFlags.Instance);
            var res = method.Invoke(_reachability, new object[1] { s });

            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            method = ReachabilityType.GetMethod("IsInterfaceValid", BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var n in nics) {
                res = method.Invoke(null, new object[1] { n });
            }

            var asender = new object();
            method = ReachabilityType.GetMethod("OnNetworkChange", BindingFlags.NonPublic | BindingFlags.Instance);
            res = method.Invoke(_reachability, new object[2] { asender, e });

            var ReplicatorType = typeof(Replicator);
            var targetEndpoint = new URLEndpoint(new Uri("ws://192.168.0.11:4984/app"));
            var config = new ReplicatorConfiguration(Db, targetEndpoint);
            Replicator replicator = new Replicator(config);
            method = ReplicatorType.GetMethod("ReachabilityChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            res = method.Invoke(replicator, new object[2] { asender, e });
        }

        [Fact]
        public void TestHttpLogic()
        {
            var logic = new HTTPLogic(new Uri("ws://192.168.0.11:4984/app"));
            var prop = HttpLogicType.GetProperty("Credential", BindingFlags.Public | BindingFlags.Instance);
            prop.SetValue(hTTPLogic, new NetworkCredential("name", "pass"));
            var method = HttpLogicType.GetMethod("CreateAuthHeader", BindingFlags.NonPublic | BindingFlags.Instance);
            var authstring = "Basic realm=\"Couchbase Sync Gateway\"";
            var res = method.Invoke(hTTPLogic, new object[1] { authstring });

            method = HttpLogicType.GetMethod("ParseAuthHeader", BindingFlags.NonPublic | BindingFlags.Instance);
            res = method.Invoke(hTTPLogic, new object[1] { authstring });

            prop = HttpLogicType.GetProperty("UseTls", BindingFlags.Public | BindingFlags.Instance);
            var useTle = logic.UseTls;

            prop = HttpLogicType.GetProperty("Error", BindingFlags.Public | BindingFlags.Instance);
            var error = logic.Error;
        }

        [Fact]
        public void TestReplicatorOptionsDictionary()
        {
            var ReplicatorOptionsDictionaryType = typeof(ReplicatorOptionsDictionary);
            var optDict = new ReplicatorOptionsDictionary();
            var cookieStr = optDict.CookieString;
            var protocol = optDict.Protocols;
            optDict.Channels = new List<string>();
            var channels = optDict.Channels;
            optDict.Filter = "filter string";
            var filter = optDict.Filter;
            optDict.FilterParams = new Dictionary<string, object>();
            var filterParam = optDict.FilterParams;
            optDict.RemoteDBUniqueID = "remote id";
            var remoteId = optDict.RemoteDBUniqueID;

            optDict.Cookies.Add(new Cookie("name", "value"));

            var method = ReplicatorOptionsDictionaryType.GetMethod("BuildInternal", BindingFlags.NonPublic | BindingFlags.Instance);
            var res = method.Invoke(optDict, null);
        }
    }
}
