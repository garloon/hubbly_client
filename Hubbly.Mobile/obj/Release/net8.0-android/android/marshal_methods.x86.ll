; ModuleID = 'marshal_methods.x86.ll'
source_filename = "marshal_methods.x86.ll"
target datalayout = "e-m:e-p:32:32-p270:32:32-p271:32:32-p272:64:64-f64:32:64-f80:32-n8:16:32-S128"
target triple = "i686-unknown-linux-android21"

%struct.MarshalMethodName = type {
	i64, ; uint64_t id
	ptr ; char* name
}

%struct.MarshalMethodsManagedClass = type {
	i32, ; uint32_t token
	ptr ; MonoClass klass
}

@assembly_image_cache = dso_local local_unnamed_addr global [167 x ptr] zeroinitializer, align 4

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [334 x i32] [
	i32 2616222, ; 0: System.Net.NetworkInformation.dll => 0x27eb9e => 130
	i32 10078191, ; 1: Serilog.Sinks.RollingFile.dll => 0x99c7ef => 69
	i32 10166715, ; 2: System.Net.NameResolution.dll => 0x9b21bb => 129
	i32 39485524, ; 3: System.Net.WebSockets.dll => 0x25a8054 => 137
	i32 42639949, ; 4: System.Threading.Thread => 0x28aa24d => 155
	i32 67008169, ; 5: zh-Hant\Microsoft.Maui.Controls.resources => 0x3fe76a9 => 33
	i32 72070932, ; 6: Microsoft.Maui.Graphics.dll => 0x44bb714 => 62
	i32 75660030, ; 7: Serilog.Sinks.Async => 0x4827afe => 67
	i32 117431740, ; 8: System.Runtime.InteropServices => 0x6ffddbc => 144
	i32 122350210, ; 9: System.Threading.Channels.dll => 0x74aea82 => 154
	i32 142721839, ; 10: System.Net.WebHeaderCollection => 0x881c32f => 135
	i32 165246403, ; 11: Xamarin.AndroidX.Collection.dll => 0x9d975c3 => 81
	i32 182336117, ; 12: Xamarin.AndroidX.SwipeRefreshLayout.dll => 0xade3a75 => 100
	i32 195452805, ; 13: vi/Microsoft.Maui.Controls.resources.dll => 0xba65f85 => 30
	i32 199333315, ; 14: zh-HK/Microsoft.Maui.Controls.resources.dll => 0xbe195c3 => 31
	i32 205061960, ; 15: System.ComponentModel => 0xc38ff48 => 114
	i32 221063263, ; 16: Microsoft.AspNetCore.Http.Connections.Client => 0xd2d285f => 39
	i32 231814094, ; 17: System.Globalization => 0xdd133ce => 118
	i32 276479776, ; 18: System.Threading.Timer.dll => 0x107abf20 => 156
	i32 280992041, ; 19: cs/Microsoft.Maui.Controls.resources.dll => 0x10bf9929 => 2
	i32 317674968, ; 20: vi\Microsoft.Maui.Controls.resources => 0x12ef55d8 => 30
	i32 318968648, ; 21: Xamarin.AndroidX.Activity.dll => 0x13031348 => 77
	i32 336156722, ; 22: ja/Microsoft.Maui.Controls.resources.dll => 0x14095832 => 15
	i32 342366114, ; 23: Xamarin.AndroidX.Lifecycle.Common => 0x146817a2 => 88
	i32 347068432, ; 24: SQLitePCLRaw.lib.e_sqlite3.android.dll => 0x14afd810 => 73
	i32 348048101, ; 25: Microsoft.AspNetCore.Http.Connections.Common.dll => 0x14becae5 => 40
	i32 356389973, ; 26: it/Microsoft.Maui.Controls.resources.dll => 0x153e1455 => 14
	i32 379916513, ; 27: System.Threading.Thread.dll => 0x16a510e1 => 155
	i32 385762202, ; 28: System.Memory.dll => 0x16fe439a => 126
	i32 395744057, ; 29: _Microsoft.Android.Resource.Designer => 0x17969339 => 34
	i32 435591531, ; 30: sv/Microsoft.Maui.Controls.resources.dll => 0x19f6996b => 26
	i32 442565967, ; 31: System.Collections => 0x1a61054f => 111
	i32 450948140, ; 32: Xamarin.AndroidX.Fragment.dll => 0x1ae0ec2c => 87
	i32 456227837, ; 33: System.Web.HttpUtility.dll => 0x1b317bfd => 158
	i32 458494020, ; 34: Microsoft.AspNetCore.SignalR.Common.dll => 0x1b541044 => 43
	i32 469710990, ; 35: System.dll => 0x1bff388e => 160
	i32 498788369, ; 36: System.ObjectModel => 0x1dbae811 => 139
	i32 500358224, ; 37: id/Microsoft.Maui.Controls.resources.dll => 0x1dd2dc50 => 13
	i32 503918385, ; 38: fi/Microsoft.Maui.Controls.resources.dll => 0x1e092f31 => 7
	i32 513247710, ; 39: Microsoft.Extensions.Primitives.dll => 0x1e9789de => 56
	i32 539058512, ; 40: Microsoft.Extensions.Logging => 0x20216150 => 52
	i32 540030774, ; 41: System.IO.FileSystem.dll => 0x20303736 => 122
	i32 545304856, ; 42: System.Runtime.Extensions => 0x2080b118 => 142
	i32 592146354, ; 43: pt-BR/Microsoft.Maui.Controls.resources.dll => 0x234b6fb2 => 21
	i32 597488923, ; 44: CommunityToolkit.Maui => 0x239cf51b => 35
	i32 627609679, ; 45: Xamarin.AndroidX.CustomView => 0x2568904f => 85
	i32 627931235, ; 46: nl\Microsoft.Maui.Controls.resources => 0x256d7863 => 19
	i32 662205335, ; 47: System.Text.Encodings.Web.dll => 0x27787397 => 151
	i32 672442732, ; 48: System.Collections.Concurrent => 0x2814a96c => 107
	i32 683518922, ; 49: System.Net.Security => 0x28bdabca => 133
	i32 688181140, ; 50: ca/Microsoft.Maui.Controls.resources.dll => 0x2904cf94 => 1
	i32 706645707, ; 51: ko/Microsoft.Maui.Controls.resources.dll => 0x2a1e8ecb => 16
	i32 709557578, ; 52: de/Microsoft.Maui.Controls.resources.dll => 0x2a4afd4a => 4
	i32 722857257, ; 53: System.Runtime.Loader.dll => 0x2b15ed29 => 145
	i32 748832960, ; 54: SQLitePCLRaw.batteries_v2 => 0x2ca248c0 => 71
	i32 759454413, ; 55: System.Net.Requests => 0x2d445acd => 132
	i32 775507847, ; 56: System.IO.Compression => 0x2e394f87 => 120
	i32 777317022, ; 57: sk\Microsoft.Maui.Controls.resources => 0x2e54ea9e => 25
	i32 789151979, ; 58: Microsoft.Extensions.Options => 0x2f0980eb => 55
	i32 812630446, ; 59: Serilog => 0x306fc1ae => 63
	i32 823281589, ; 60: System.Private.Uri.dll => 0x311247b5 => 140
	i32 830298997, ; 61: System.IO.Compression.Brotli => 0x317d5b75 => 119
	i32 832711436, ; 62: Microsoft.AspNetCore.SignalR.Protocols.Json.dll => 0x31a22b0c => 44
	i32 877678880, ; 63: System.Globalization.dll => 0x34505120 => 118
	i32 904024072, ; 64: System.ComponentModel.Primitives.dll => 0x35e25008 => 112
	i32 926902833, ; 65: tr/Microsoft.Maui.Controls.resources.dll => 0x373f6a31 => 28
	i32 967690846, ; 66: Xamarin.AndroidX.Lifecycle.Common.dll => 0x39adca5e => 88
	i32 992768348, ; 67: System.Collections.dll => 0x3b2c715c => 111
	i32 994442037, ; 68: System.IO.FileSystem => 0x3b45fb35 => 122
	i32 1012816738, ; 69: Xamarin.AndroidX.SavedState.dll => 0x3c5e5b62 => 98
	i32 1028951442, ; 70: Microsoft.Extensions.DependencyInjection.Abstractions => 0x3d548d92 => 49
	i32 1029334545, ; 71: da/Microsoft.Maui.Controls.resources.dll => 0x3d5a6611 => 3
	i32 1035644815, ; 72: Xamarin.AndroidX.AppCompat => 0x3dbaaf8f => 78
	i32 1044663988, ; 73: System.Linq.Expressions.dll => 0x3e444eb4 => 124
	i32 1052210849, ; 74: Xamarin.AndroidX.Lifecycle.ViewModel.dll => 0x3eb776a1 => 90
	i32 1058641855, ; 75: Microsoft.AspNetCore.Http.Connections.Common => 0x3f1997bf => 40
	i32 1082857460, ; 76: System.ComponentModel.TypeConverter => 0x408b17f4 => 113
	i32 1084122840, ; 77: Xamarin.Kotlin.StdLib => 0x409e66d8 => 104
	i32 1098259244, ; 78: System => 0x41761b2c => 160
	i32 1110309514, ; 79: Microsoft.Extensions.Hosting.Abstractions => 0x422dfa8a => 51
	i32 1118262833, ; 80: ko\Microsoft.Maui.Controls.resources => 0x42a75631 => 16
	i32 1127624469, ; 81: Microsoft.Extensions.Logging.Debug => 0x43362f15 => 54
	i32 1168523401, ; 82: pt\Microsoft.Maui.Controls.resources => 0x45a64089 => 22
	i32 1178241025, ; 83: Xamarin.AndroidX.Navigation.Runtime.dll => 0x463a8801 => 95
	i32 1203215381, ; 84: pl/Microsoft.Maui.Controls.resources.dll => 0x47b79c15 => 20
	i32 1214827643, ; 85: CommunityToolkit.Mvvm => 0x4868cc7b => 37
	i32 1233093933, ; 86: Microsoft.AspNetCore.SignalR.Client.Core.dll => 0x497f852d => 42
	i32 1234928153, ; 87: nb/Microsoft.Maui.Controls.resources.dll => 0x499b8219 => 18
	i32 1260983243, ; 88: cs\Microsoft.Maui.Controls.resources => 0x4b2913cb => 2
	i32 1292207520, ; 89: SQLitePCLRaw.core.dll => 0x4d0585a0 => 72
	i32 1293217323, ; 90: Xamarin.AndroidX.DrawerLayout.dll => 0x4d14ee2b => 86
	i32 1322857724, ; 91: Serilog.Sinks.File.dll => 0x4ed934fc => 68
	i32 1324164729, ; 92: System.Linq => 0x4eed2679 => 125
	i32 1364015309, ; 93: System.IO => 0x514d38cd => 123
	i32 1373134921, ; 94: zh-Hans\Microsoft.Maui.Controls.resources => 0x51d86049 => 32
	i32 1376866003, ; 95: Xamarin.AndroidX.SavedState => 0x52114ed3 => 98
	i32 1406073936, ; 96: Xamarin.AndroidX.CoordinatorLayout => 0x53cefc50 => 82
	i32 1414043276, ; 97: Microsoft.AspNetCore.Connections.Abstractions.dll => 0x5448968c => 38
	i32 1430672901, ; 98: ar\Microsoft.Maui.Controls.resources => 0x55465605 => 0
	i32 1452070440, ; 99: System.Formats.Asn1.dll => 0x568cd628 => 117
	i32 1457743152, ; 100: System.Runtime.Extensions.dll => 0x56e36530 => 142
	i32 1458022317, ; 101: System.Net.Security.dll => 0x56e7a7ad => 133
	i32 1461004990, ; 102: es\Microsoft.Maui.Controls.resources => 0x57152abe => 6
	i32 1461234159, ; 103: System.Collections.Immutable.dll => 0x5718a9ef => 108
	i32 1462112819, ; 104: System.IO.Compression.dll => 0x57261233 => 120
	i32 1469204771, ; 105: Xamarin.AndroidX.AppCompat.AppCompatResources => 0x57924923 => 79
	i32 1470490898, ; 106: Microsoft.Extensions.Primitives => 0x57a5e912 => 56
	i32 1479771757, ; 107: System.Collections.Immutable => 0x5833866d => 108
	i32 1480492111, ; 108: System.IO.Compression.Brotli.dll => 0x583e844f => 119
	i32 1493001747, ; 109: hi/Microsoft.Maui.Controls.resources.dll => 0x58fd6613 => 10
	i32 1514721132, ; 110: el/Microsoft.Maui.Controls.resources.dll => 0x5a48cf6c => 5
	i32 1543031311, ; 111: System.Text.RegularExpressions.dll => 0x5bf8ca0f => 153
	i32 1551623176, ; 112: sk/Microsoft.Maui.Controls.resources.dll => 0x5c7be408 => 25
	i32 1565862583, ; 113: System.IO.FileSystem.Primitives => 0x5d552ab7 => 121
	i32 1589949322, ; 114: Serilog.Formatting.Compact => 0x5ec4b38a => 66
	i32 1618516317, ; 115: System.Net.WebSockets.Client.dll => 0x6078995d => 136
	i32 1622152042, ; 116: Xamarin.AndroidX.Loader.dll => 0x60b0136a => 92
	i32 1624863272, ; 117: Xamarin.AndroidX.ViewPager2 => 0x60d97228 => 102
	i32 1625558452, ; 118: Serilog.dll => 0x60e40db4 => 63
	i32 1634654947, ; 119: CommunityToolkit.Maui.Core.dll => 0x616edae3 => 36
	i32 1636350590, ; 120: Xamarin.AndroidX.CursorAdapter => 0x6188ba7e => 84
	i32 1639515021, ; 121: System.Net.Http.dll => 0x61b9038d => 127
	i32 1639986890, ; 122: System.Text.RegularExpressions => 0x61c036ca => 153
	i32 1657153582, ; 123: System.Runtime => 0x62c6282e => 147
	i32 1658251792, ; 124: Xamarin.Google.Android.Material.dll => 0x62d6ea10 => 103
	i32 1677501392, ; 125: System.Net.Primitives.dll => 0x63fca3d0 => 131
	i32 1678508291, ; 126: System.Net.WebSockets => 0x640c0103 => 137
	i32 1679769178, ; 127: System.Security.Cryptography => 0x641f3e5a => 148
	i32 1711441057, ; 128: SQLitePCLRaw.lib.e_sqlite3.android => 0x660284a1 => 73
	i32 1729485958, ; 129: Xamarin.AndroidX.CardView.dll => 0x6715dc86 => 80
	i32 1736233607, ; 130: ro/Microsoft.Maui.Controls.resources.dll => 0x677cd287 => 23
	i32 1743415430, ; 131: ca\Microsoft.Maui.Controls.resources => 0x67ea6886 => 1
	i32 1746115085, ; 132: System.IO.Pipelines.dll => 0x68139a0d => 76
	i32 1746316138, ; 133: Mono.Android.Export => 0x6816ab6a => 164
	i32 1763938596, ; 134: System.Diagnostics.TraceSource.dll => 0x69239124 => 116
	i32 1766324549, ; 135: Xamarin.AndroidX.SwipeRefreshLayout => 0x6947f945 => 100
	i32 1770582343, ; 136: Microsoft.Extensions.Logging.dll => 0x6988f147 => 52
	i32 1780572499, ; 137: Mono.Android.Runtime.dll => 0x6a216153 => 165
	i32 1782862114, ; 138: ms\Microsoft.Maui.Controls.resources => 0x6a445122 => 17
	i32 1788241197, ; 139: Xamarin.AndroidX.Fragment => 0x6a96652d => 87
	i32 1793755602, ; 140: he\Microsoft.Maui.Controls.resources => 0x6aea89d2 => 9
	i32 1808609942, ; 141: Xamarin.AndroidX.Loader => 0x6bcd3296 => 92
	i32 1813058853, ; 142: Xamarin.Kotlin.StdLib.dll => 0x6c111525 => 104
	i32 1813201214, ; 143: Xamarin.Google.Android.Material => 0x6c13413e => 103
	i32 1818569960, ; 144: Xamarin.AndroidX.Navigation.UI.dll => 0x6c652ce8 => 96
	i32 1824175904, ; 145: System.Text.Encoding.Extensions => 0x6cbab720 => 149
	i32 1828688058, ; 146: Microsoft.Extensions.Logging.Abstractions.dll => 0x6cff90ba => 53
	i32 1842015223, ; 147: uk/Microsoft.Maui.Controls.resources.dll => 0x6dcaebf7 => 29
	i32 1853025655, ; 148: sv\Microsoft.Maui.Controls.resources => 0x6e72ed77 => 26
	i32 1858542181, ; 149: System.Linq.Expressions => 0x6ec71a65 => 124
	i32 1875935024, ; 150: fr\Microsoft.Maui.Controls.resources => 0x6fd07f30 => 8
	i32 1910275211, ; 151: System.Collections.NonGeneric.dll => 0x71dc7c8b => 109
	i32 1945717188, ; 152: Microsoft.AspNetCore.SignalR.Client.Core => 0x73f949c4 => 42
	i32 1961813231, ; 153: Xamarin.AndroidX.Security.SecurityCrypto.dll => 0x74eee4ef => 99
	i32 1967334205, ; 154: Microsoft.AspNetCore.SignalR.Common => 0x7543233d => 43
	i32 1968388702, ; 155: Microsoft.Extensions.Configuration.dll => 0x75533a5e => 45
	i32 2003115576, ; 156: el\Microsoft.Maui.Controls.resources => 0x77651e38 => 5
	i32 2019465201, ; 157: Xamarin.AndroidX.Lifecycle.ViewModel => 0x785e97f1 => 90
	i32 2025202353, ; 158: ar/Microsoft.Maui.Controls.resources.dll => 0x78b622b1 => 0
	i32 2045470958, ; 159: System.Private.Xml => 0x79eb68ee => 141
	i32 2048278909, ; 160: Microsoft.Extensions.Configuration.Binder.dll => 0x7a16417d => 47
	i32 2055257422, ; 161: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 0x7a80bd4e => 89
	i32 2066184531, ; 162: de\Microsoft.Maui.Controls.resources => 0x7b277953 => 4
	i32 2070888862, ; 163: System.Diagnostics.TraceSource => 0x7b6f419e => 116
	i32 2079903147, ; 164: System.Runtime.dll => 0x7bf8cdab => 147
	i32 2090596640, ; 165: System.Numerics.Vectors => 0x7c9bf920 => 138
	i32 2103459038, ; 166: SQLitePCLRaw.provider.e_sqlite3.dll => 0x7d603cde => 74
	i32 2127167465, ; 167: System.Console => 0x7ec9ffe9 => 115
	i32 2142473426, ; 168: System.Collections.Specialized => 0x7fb38cd2 => 110
	i32 2159891885, ; 169: Microsoft.Maui => 0x80bd55ad => 60
	i32 2169148018, ; 170: hu\Microsoft.Maui.Controls.resources => 0x814a9272 => 12
	i32 2181485124, ; 171: Serilog.Sinks.File => 0x8206d244 => 68
	i32 2181898931, ; 172: Microsoft.Extensions.Options.dll => 0x820d22b3 => 55
	i32 2192057212, ; 173: Microsoft.Extensions.Logging.Abstractions => 0x82a8237c => 53
	i32 2193016926, ; 174: System.ObjectModel.dll => 0x82b6c85e => 139
	i32 2201107256, ; 175: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 0x83323b38 => 105
	i32 2201231467, ; 176: System.Net.Http => 0x8334206b => 127
	i32 2207618523, ; 177: it\Microsoft.Maui.Controls.resources => 0x839595db => 14
	i32 2229158877, ; 178: Microsoft.Extensions.Features.dll => 0x84de43dd => 50
	i32 2266799131, ; 179: Microsoft.Extensions.Configuration.Abstractions => 0x871c9c1b => 46
	i32 2270573516, ; 180: fr/Microsoft.Maui.Controls.resources.dll => 0x875633cc => 8
	i32 2279755925, ; 181: Xamarin.AndroidX.RecyclerView.dll => 0x87e25095 => 97
	i32 2295906218, ; 182: System.Net.Sockets => 0x88d8bfaa => 134
	i32 2298471582, ; 183: System.Net.Mail => 0x88ffe49e => 128
	i32 2303942373, ; 184: nb\Microsoft.Maui.Controls.resources => 0x89535ee5 => 18
	i32 2305521784, ; 185: System.Private.CoreLib.dll => 0x896b7878 => 162
	i32 2317568709, ; 186: Serilog.Sinks.Async.dll => 0x8a234ac5 => 67
	i32 2319144366, ; 187: Microsoft.AspNetCore.SignalR.Client => 0x8a3b55ae => 41
	i32 2340441535, ; 188: System.Runtime.InteropServices.RuntimeInformation.dll => 0x8b804dbf => 143
	i32 2353062107, ; 189: System.Net.Primitives => 0x8c40e0db => 131
	i32 2358249420, ; 190: Serilog.Extensions.Logging => 0x8c9007cc => 64
	i32 2368005991, ; 191: System.Xml.ReaderWriter.dll => 0x8d24e767 => 159
	i32 2371007202, ; 192: Microsoft.Extensions.Configuration => 0x8d52b2e2 => 45
	i32 2395872292, ; 193: id\Microsoft.Maui.Controls.resources => 0x8ece1c24 => 13
	i32 2401565422, ; 194: System.Web.HttpUtility => 0x8f24faee => 158
	i32 2427813419, ; 195: hi\Microsoft.Maui.Controls.resources => 0x90b57e2b => 10
	i32 2435356389, ; 196: System.Console.dll => 0x912896e5 => 115
	i32 2454642406, ; 197: System.Text.Encoding.dll => 0x924edee6 => 150
	i32 2458678730, ; 198: System.Net.Sockets.dll => 0x928c75ca => 134
	i32 2465273461, ; 199: SQLitePCLRaw.batteries_v2.dll => 0x92f11675 => 71
	i32 2471841756, ; 200: netstandard.dll => 0x93554fdc => 161
	i32 2475788418, ; 201: Java.Interop.dll => 0x93918882 => 163
	i32 2480646305, ; 202: Microsoft.Maui.Controls => 0x93dba8a1 => 58
	i32 2550873716, ; 203: hr\Microsoft.Maui.Controls.resources => 0x980b3e74 => 11
	i32 2570120770, ; 204: System.Text.Encodings.Web => 0x9930ee42 => 151
	i32 2585220780, ; 205: System.Text.Encoding.Extensions.dll => 0x9a1756ac => 149
	i32 2593496499, ; 206: pl\Microsoft.Maui.Controls.resources => 0x9a959db3 => 20
	i32 2605712449, ; 207: Xamarin.KotlinX.Coroutines.Core.Jvm => 0x9b500441 => 105
	i32 2616218305, ; 208: Microsoft.Extensions.Logging.Debug.dll => 0x9bf052c1 => 54
	i32 2617129537, ; 209: System.Private.Xml.dll => 0x9bfe3a41 => 141
	i32 2620871830, ; 210: Xamarin.AndroidX.CursorAdapter.dll => 0x9c375496 => 84
	i32 2626831493, ; 211: ja\Microsoft.Maui.Controls.resources => 0x9c924485 => 15
	i32 2627802292, ; 212: Serilog.Extensions.Logging.dll => 0x9ca114b4 => 64
	i32 2637500010, ; 213: Microsoft.Extensions.Features => 0x9d350e6a => 50
	i32 2663698177, ; 214: System.Runtime.Loader => 0x9ec4cf01 => 145
	i32 2693849962, ; 215: System.IO.dll => 0xa090e36a => 123
	i32 2724373263, ; 216: System.Runtime.Numerics.dll => 0xa262a30f => 146
	i32 2732626843, ; 217: Xamarin.AndroidX.Activity => 0xa2e0939b => 77
	i32 2735172069, ; 218: System.Threading.Channels => 0xa30769e5 => 154
	i32 2737747696, ; 219: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 0xa32eb6f0 => 79
	i32 2752995522, ; 220: pt-BR\Microsoft.Maui.Controls.resources => 0xa41760c2 => 21
	i32 2758225723, ; 221: Microsoft.Maui.Controls.Xaml => 0xa4672f3b => 59
	i32 2764765095, ; 222: Microsoft.Maui.dll => 0xa4caf7a7 => 60
	i32 2778768386, ; 223: Xamarin.AndroidX.ViewPager.dll => 0xa5a0a402 => 101
	i32 2785988530, ; 224: th\Microsoft.Maui.Controls.resources => 0xa60ecfb2 => 27
	i32 2801831435, ; 225: Microsoft.Maui.Graphics => 0xa7008e0b => 62
	i32 2806116107, ; 226: es/Microsoft.Maui.Controls.resources.dll => 0xa741ef0b => 6
	i32 2810250172, ; 227: Xamarin.AndroidX.CoordinatorLayout.dll => 0xa78103bc => 82
	i32 2830523736, ; 228: Serilog.Formatting.Compact.dll => 0xa8b65d58 => 66
	i32 2831556043, ; 229: nl/Microsoft.Maui.Controls.resources.dll => 0xa8c61dcb => 19
	i32 2833578113, ; 230: Serilog.Extensions.Logging.File.dll => 0xa8e4f881 => 65
	i32 2853208004, ; 231: Xamarin.AndroidX.ViewPager => 0xaa107fc4 => 101
	i32 2861098320, ; 232: Mono.Android.Export.dll => 0xaa88e550 => 164
	i32 2861189240, ; 233: Microsoft.Maui.Essentials => 0xaa8a4878 => 61
	i32 2868488919, ; 234: CommunityToolkit.Maui.Core => 0xaaf9aad7 => 36
	i32 2875347124, ; 235: Microsoft.AspNetCore.Http.Connections.Client.dll => 0xab6250b4 => 39
	i32 2909740682, ; 236: System.Private.CoreLib => 0xad6f1e8a => 162
	i32 2916838712, ; 237: Xamarin.AndroidX.ViewPager2.dll => 0xaddb6d38 => 102
	i32 2919462931, ; 238: System.Numerics.Vectors.dll => 0xae037813 => 138
	i32 2959614098, ; 239: System.ComponentModel.dll => 0xb0682092 => 114
	i32 2978675010, ; 240: Xamarin.AndroidX.DrawerLayout => 0xb18af942 => 86
	i32 2987532451, ; 241: Xamarin.AndroidX.Security.SecurityCrypto => 0xb21220a3 => 99
	i32 3038032645, ; 242: _Microsoft.Android.Resource.Designer.dll => 0xb514b305 => 34
	i32 3057625584, ; 243: Xamarin.AndroidX.Navigation.Common => 0xb63fa9f0 => 93
	i32 3059408633, ; 244: Mono.Android.Runtime => 0xb65adef9 => 165
	i32 3059793426, ; 245: System.ComponentModel.Primitives => 0xb660be12 => 112
	i32 3077302341, ; 246: hu/Microsoft.Maui.Controls.resources.dll => 0xb76be845 => 12
	i32 3103600923, ; 247: System.Formats.Asn1 => 0xb8fd311b => 117
	i32 3178803400, ; 248: Xamarin.AndroidX.Navigation.Fragment.dll => 0xbd78b0c8 => 94
	i32 3201398711, ; 249: Serilog.Sinks.RollingFile => 0xbed177b7 => 69
	i32 3220365878, ; 250: System.Threading => 0xbff2e236 => 157
	i32 3258312781, ; 251: Xamarin.AndroidX.CardView => 0xc235e84d => 80
	i32 3286872994, ; 252: SQLite-net.dll => 0xc3e9b3a2 => 70
	i32 3299363146, ; 253: System.Text.Encoding => 0xc4a8494a => 150
	i32 3305363605, ; 254: fi\Microsoft.Maui.Controls.resources => 0xc503d895 => 7
	i32 3316684772, ; 255: System.Net.Requests.dll => 0xc5b097e4 => 132
	i32 3317135071, ; 256: Xamarin.AndroidX.CustomView.dll => 0xc5b776df => 85
	i32 3346324047, ; 257: Xamarin.AndroidX.Navigation.Runtime => 0xc774da4f => 95
	i32 3357674450, ; 258: ru\Microsoft.Maui.Controls.resources => 0xc8220bd2 => 24
	i32 3358260929, ; 259: System.Text.Json => 0xc82afec1 => 152
	i32 3360279109, ; 260: SQLitePCLRaw.core => 0xc849ca45 => 72
	i32 3361686909, ; 261: Serilog.Extensions.Logging.File => 0xc85f457d => 65
	i32 3362522851, ; 262: Xamarin.AndroidX.Core => 0xc86c06e3 => 83
	i32 3366347497, ; 263: Java.Interop => 0xc8a662e9 => 163
	i32 3374999561, ; 264: Xamarin.AndroidX.RecyclerView => 0xc92a6809 => 97
	i32 3381016424, ; 265: da\Microsoft.Maui.Controls.resources => 0xc9863768 => 3
	i32 3421170118, ; 266: Microsoft.Extensions.Configuration.Binder => 0xcbeae9c6 => 47
	i32 3428513518, ; 267: Microsoft.Extensions.DependencyInjection.dll => 0xcc5af6ee => 48
	i32 3430777524, ; 268: netstandard => 0xcc7d82b4 => 161
	i32 3452344032, ; 269: Microsoft.Maui.Controls.Compatibility.dll => 0xcdc696e0 => 57
	i32 3463511458, ; 270: hr/Microsoft.Maui.Controls.resources.dll => 0xce70fda2 => 11
	i32 3466904072, ; 271: Microsoft.AspNetCore.SignalR.Client.dll => 0xcea4c208 => 41
	i32 3471940407, ; 272: System.ComponentModel.TypeConverter.dll => 0xcef19b37 => 113
	i32 3476120550, ; 273: Mono.Android => 0xcf3163e6 => 166
	i32 3479583265, ; 274: ru/Microsoft.Maui.Controls.resources.dll => 0xcf663a21 => 24
	i32 3484440000, ; 275: ro\Microsoft.Maui.Controls.resources => 0xcfb055c0 => 23
	i32 3485117614, ; 276: System.Text.Json.dll => 0xcfbaacae => 152
	i32 3560100363, ; 277: System.Threading.Timer => 0xd432d20b => 156
	i32 3580758918, ; 278: zh-HK\Microsoft.Maui.Controls.resources => 0xd56e0b86 => 31
	i32 3598340787, ; 279: System.Net.WebSockets.Client => 0xd67a52b3 => 136
	i32 3608519521, ; 280: System.Linq.dll => 0xd715a361 => 125
	i32 3624195450, ; 281: System.Runtime.InteropServices.RuntimeInformation => 0xd804d57a => 143
	i32 3638274909, ; 282: System.IO.FileSystem.Primitives.dll => 0xd8dbab5d => 121
	i32 3641597786, ; 283: Xamarin.AndroidX.Lifecycle.LiveData.Core => 0xd90e5f5a => 89
	i32 3643446276, ; 284: tr\Microsoft.Maui.Controls.resources => 0xd92a9404 => 28
	i32 3643854240, ; 285: Xamarin.AndroidX.Navigation.Fragment => 0xd930cda0 => 94
	i32 3657292374, ; 286: Microsoft.Extensions.Configuration.Abstractions.dll => 0xd9fdda56 => 46
	i32 3660523487, ; 287: System.Net.NetworkInformation => 0xda2f27df => 130
	i32 3672681054, ; 288: Mono.Android.dll => 0xdae8aa5e => 166
	i32 3691870036, ; 289: Microsoft.AspNetCore.SignalR.Protocols.Json => 0xdc0d7754 => 44
	i32 3697841164, ; 290: zh-Hant/Microsoft.Maui.Controls.resources.dll => 0xdc68940c => 33
	i32 3723224191, ; 291: Hubbly.Mobile => 0xddebe47f => 106
	i32 3724971120, ; 292: Xamarin.AndroidX.Navigation.Common.dll => 0xde068c70 => 93
	i32 3732100267, ; 293: System.Net.NameResolution => 0xde7354ab => 129
	i32 3748608112, ; 294: System.Diagnostics.DiagnosticSource => 0xdf6f3870 => 75
	i32 3754567612, ; 295: SQLitePCLRaw.provider.e_sqlite3 => 0xdfca27bc => 74
	i32 3786282454, ; 296: Xamarin.AndroidX.Collection => 0xe1ae15d6 => 81
	i32 3787005001, ; 297: Microsoft.AspNetCore.Connections.Abstractions => 0xe1b91c49 => 38
	i32 3792276235, ; 298: System.Collections.NonGeneric => 0xe2098b0b => 109
	i32 3800979733, ; 299: Microsoft.Maui.Controls.Compatibility => 0xe28e5915 => 57
	i32 3802395368, ; 300: System.Collections.Specialized.dll => 0xe2a3f2e8 => 110
	i32 3817368567, ; 301: CommunityToolkit.Maui.dll => 0xe3886bf7 => 35
	i32 3823082795, ; 302: System.Security.Cryptography.dll => 0xe3df9d2b => 148
	i32 3841636137, ; 303: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 0xe4fab729 => 49
	i32 3844307129, ; 304: System.Net.Mail.dll => 0xe52378b9 => 128
	i32 3849253459, ; 305: System.Runtime.InteropServices.dll => 0xe56ef253 => 144
	i32 3876362041, ; 306: SQLite-net => 0xe70c9739 => 70
	i32 3885497537, ; 307: System.Net.WebHeaderCollection.dll => 0xe797fcc1 => 135
	i32 3889960447, ; 308: zh-Hans/Microsoft.Maui.Controls.resources.dll => 0xe7dc15ff => 32
	i32 3896106733, ; 309: System.Collections.Concurrent.dll => 0xe839deed => 107
	i32 3896760992, ; 310: Xamarin.AndroidX.Core.dll => 0xe843daa0 => 83
	i32 3916384822, ; 311: Hubbly.Mobile.dll => 0xe96f4a36 => 106
	i32 3928044579, ; 312: System.Xml.ReaderWriter => 0xea213423 => 159
	i32 3931092270, ; 313: Xamarin.AndroidX.Navigation.UI => 0xea4fb52e => 96
	i32 3955647286, ; 314: Xamarin.AndroidX.AppCompat.dll => 0xebc66336 => 78
	i32 3980434154, ; 315: th/Microsoft.Maui.Controls.resources.dll => 0xed409aea => 27
	i32 3987592930, ; 316: he/Microsoft.Maui.Controls.resources.dll => 0xedadd6e2 => 9
	i32 4023392905, ; 317: System.IO.Pipelines => 0xefd01a89 => 76
	i32 4025784931, ; 318: System.Memory => 0xeff49a63 => 126
	i32 4046471985, ; 319: Microsoft.Maui.Controls.Xaml.dll => 0xf1304331 => 59
	i32 4073602200, ; 320: System.Threading.dll => 0xf2ce3c98 => 157
	i32 4078967171, ; 321: Microsoft.Extensions.Hosting.Abstractions.dll => 0xf3201983 => 51
	i32 4094352644, ; 322: Microsoft.Maui.Essentials.dll => 0xf40add04 => 61
	i32 4100113165, ; 323: System.Private.Uri => 0xf462c30d => 140
	i32 4102112229, ; 324: pt/Microsoft.Maui.Controls.resources.dll => 0xf48143e5 => 22
	i32 4125707920, ; 325: ms/Microsoft.Maui.Controls.resources.dll => 0xf5e94e90 => 17
	i32 4126470640, ; 326: Microsoft.Extensions.DependencyInjection => 0xf5f4f1f0 => 48
	i32 4150914736, ; 327: uk\Microsoft.Maui.Controls.resources => 0xf769eeb0 => 29
	i32 4182413190, ; 328: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 0xf94a8f86 => 91
	i32 4213026141, ; 329: System.Diagnostics.DiagnosticSource.dll => 0xfb1dad5d => 75
	i32 4271975918, ; 330: Microsoft.Maui.Controls.dll => 0xfea12dee => 58
	i32 4274623895, ; 331: CommunityToolkit.Mvvm.dll => 0xfec99597 => 37
	i32 4274976490, ; 332: System.Runtime.Numerics => 0xfecef6ea => 146
	i32 4292120959 ; 333: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 0xffd4917f => 91
], align 4

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [334 x i32] [
	i32 130, ; 0
	i32 69, ; 1
	i32 129, ; 2
	i32 137, ; 3
	i32 155, ; 4
	i32 33, ; 5
	i32 62, ; 6
	i32 67, ; 7
	i32 144, ; 8
	i32 154, ; 9
	i32 135, ; 10
	i32 81, ; 11
	i32 100, ; 12
	i32 30, ; 13
	i32 31, ; 14
	i32 114, ; 15
	i32 39, ; 16
	i32 118, ; 17
	i32 156, ; 18
	i32 2, ; 19
	i32 30, ; 20
	i32 77, ; 21
	i32 15, ; 22
	i32 88, ; 23
	i32 73, ; 24
	i32 40, ; 25
	i32 14, ; 26
	i32 155, ; 27
	i32 126, ; 28
	i32 34, ; 29
	i32 26, ; 30
	i32 111, ; 31
	i32 87, ; 32
	i32 158, ; 33
	i32 43, ; 34
	i32 160, ; 35
	i32 139, ; 36
	i32 13, ; 37
	i32 7, ; 38
	i32 56, ; 39
	i32 52, ; 40
	i32 122, ; 41
	i32 142, ; 42
	i32 21, ; 43
	i32 35, ; 44
	i32 85, ; 45
	i32 19, ; 46
	i32 151, ; 47
	i32 107, ; 48
	i32 133, ; 49
	i32 1, ; 50
	i32 16, ; 51
	i32 4, ; 52
	i32 145, ; 53
	i32 71, ; 54
	i32 132, ; 55
	i32 120, ; 56
	i32 25, ; 57
	i32 55, ; 58
	i32 63, ; 59
	i32 140, ; 60
	i32 119, ; 61
	i32 44, ; 62
	i32 118, ; 63
	i32 112, ; 64
	i32 28, ; 65
	i32 88, ; 66
	i32 111, ; 67
	i32 122, ; 68
	i32 98, ; 69
	i32 49, ; 70
	i32 3, ; 71
	i32 78, ; 72
	i32 124, ; 73
	i32 90, ; 74
	i32 40, ; 75
	i32 113, ; 76
	i32 104, ; 77
	i32 160, ; 78
	i32 51, ; 79
	i32 16, ; 80
	i32 54, ; 81
	i32 22, ; 82
	i32 95, ; 83
	i32 20, ; 84
	i32 37, ; 85
	i32 42, ; 86
	i32 18, ; 87
	i32 2, ; 88
	i32 72, ; 89
	i32 86, ; 90
	i32 68, ; 91
	i32 125, ; 92
	i32 123, ; 93
	i32 32, ; 94
	i32 98, ; 95
	i32 82, ; 96
	i32 38, ; 97
	i32 0, ; 98
	i32 117, ; 99
	i32 142, ; 100
	i32 133, ; 101
	i32 6, ; 102
	i32 108, ; 103
	i32 120, ; 104
	i32 79, ; 105
	i32 56, ; 106
	i32 108, ; 107
	i32 119, ; 108
	i32 10, ; 109
	i32 5, ; 110
	i32 153, ; 111
	i32 25, ; 112
	i32 121, ; 113
	i32 66, ; 114
	i32 136, ; 115
	i32 92, ; 116
	i32 102, ; 117
	i32 63, ; 118
	i32 36, ; 119
	i32 84, ; 120
	i32 127, ; 121
	i32 153, ; 122
	i32 147, ; 123
	i32 103, ; 124
	i32 131, ; 125
	i32 137, ; 126
	i32 148, ; 127
	i32 73, ; 128
	i32 80, ; 129
	i32 23, ; 130
	i32 1, ; 131
	i32 76, ; 132
	i32 164, ; 133
	i32 116, ; 134
	i32 100, ; 135
	i32 52, ; 136
	i32 165, ; 137
	i32 17, ; 138
	i32 87, ; 139
	i32 9, ; 140
	i32 92, ; 141
	i32 104, ; 142
	i32 103, ; 143
	i32 96, ; 144
	i32 149, ; 145
	i32 53, ; 146
	i32 29, ; 147
	i32 26, ; 148
	i32 124, ; 149
	i32 8, ; 150
	i32 109, ; 151
	i32 42, ; 152
	i32 99, ; 153
	i32 43, ; 154
	i32 45, ; 155
	i32 5, ; 156
	i32 90, ; 157
	i32 0, ; 158
	i32 141, ; 159
	i32 47, ; 160
	i32 89, ; 161
	i32 4, ; 162
	i32 116, ; 163
	i32 147, ; 164
	i32 138, ; 165
	i32 74, ; 166
	i32 115, ; 167
	i32 110, ; 168
	i32 60, ; 169
	i32 12, ; 170
	i32 68, ; 171
	i32 55, ; 172
	i32 53, ; 173
	i32 139, ; 174
	i32 105, ; 175
	i32 127, ; 176
	i32 14, ; 177
	i32 50, ; 178
	i32 46, ; 179
	i32 8, ; 180
	i32 97, ; 181
	i32 134, ; 182
	i32 128, ; 183
	i32 18, ; 184
	i32 162, ; 185
	i32 67, ; 186
	i32 41, ; 187
	i32 143, ; 188
	i32 131, ; 189
	i32 64, ; 190
	i32 159, ; 191
	i32 45, ; 192
	i32 13, ; 193
	i32 158, ; 194
	i32 10, ; 195
	i32 115, ; 196
	i32 150, ; 197
	i32 134, ; 198
	i32 71, ; 199
	i32 161, ; 200
	i32 163, ; 201
	i32 58, ; 202
	i32 11, ; 203
	i32 151, ; 204
	i32 149, ; 205
	i32 20, ; 206
	i32 105, ; 207
	i32 54, ; 208
	i32 141, ; 209
	i32 84, ; 210
	i32 15, ; 211
	i32 64, ; 212
	i32 50, ; 213
	i32 145, ; 214
	i32 123, ; 215
	i32 146, ; 216
	i32 77, ; 217
	i32 154, ; 218
	i32 79, ; 219
	i32 21, ; 220
	i32 59, ; 221
	i32 60, ; 222
	i32 101, ; 223
	i32 27, ; 224
	i32 62, ; 225
	i32 6, ; 226
	i32 82, ; 227
	i32 66, ; 228
	i32 19, ; 229
	i32 65, ; 230
	i32 101, ; 231
	i32 164, ; 232
	i32 61, ; 233
	i32 36, ; 234
	i32 39, ; 235
	i32 162, ; 236
	i32 102, ; 237
	i32 138, ; 238
	i32 114, ; 239
	i32 86, ; 240
	i32 99, ; 241
	i32 34, ; 242
	i32 93, ; 243
	i32 165, ; 244
	i32 112, ; 245
	i32 12, ; 246
	i32 117, ; 247
	i32 94, ; 248
	i32 69, ; 249
	i32 157, ; 250
	i32 80, ; 251
	i32 70, ; 252
	i32 150, ; 253
	i32 7, ; 254
	i32 132, ; 255
	i32 85, ; 256
	i32 95, ; 257
	i32 24, ; 258
	i32 152, ; 259
	i32 72, ; 260
	i32 65, ; 261
	i32 83, ; 262
	i32 163, ; 263
	i32 97, ; 264
	i32 3, ; 265
	i32 47, ; 266
	i32 48, ; 267
	i32 161, ; 268
	i32 57, ; 269
	i32 11, ; 270
	i32 41, ; 271
	i32 113, ; 272
	i32 166, ; 273
	i32 24, ; 274
	i32 23, ; 275
	i32 152, ; 276
	i32 156, ; 277
	i32 31, ; 278
	i32 136, ; 279
	i32 125, ; 280
	i32 143, ; 281
	i32 121, ; 282
	i32 89, ; 283
	i32 28, ; 284
	i32 94, ; 285
	i32 46, ; 286
	i32 130, ; 287
	i32 166, ; 288
	i32 44, ; 289
	i32 33, ; 290
	i32 106, ; 291
	i32 93, ; 292
	i32 129, ; 293
	i32 75, ; 294
	i32 74, ; 295
	i32 81, ; 296
	i32 38, ; 297
	i32 109, ; 298
	i32 57, ; 299
	i32 110, ; 300
	i32 35, ; 301
	i32 148, ; 302
	i32 49, ; 303
	i32 128, ; 304
	i32 144, ; 305
	i32 70, ; 306
	i32 135, ; 307
	i32 32, ; 308
	i32 107, ; 309
	i32 83, ; 310
	i32 106, ; 311
	i32 159, ; 312
	i32 96, ; 313
	i32 78, ; 314
	i32 27, ; 315
	i32 9, ; 316
	i32 76, ; 317
	i32 126, ; 318
	i32 59, ; 319
	i32 157, ; 320
	i32 51, ; 321
	i32 61, ; 322
	i32 140, ; 323
	i32 22, ; 324
	i32 17, ; 325
	i32 48, ; 326
	i32 29, ; 327
	i32 91, ; 328
	i32 75, ; 329
	i32 58, ; 330
	i32 37, ; 331
	i32 146, ; 332
	i32 91 ; 333
], align 4

@marshal_methods_number_of_classes = dso_local local_unnamed_addr constant i32 0, align 4

@marshal_methods_class_cache = dso_local local_unnamed_addr global [0 x %struct.MarshalMethodsManagedClass] zeroinitializer, align 4

; Names of classes in which marshal methods reside
@mm_class_names = dso_local local_unnamed_addr constant [0 x ptr] zeroinitializer, align 4

@mm_method_names = dso_local local_unnamed_addr constant [1 x %struct.MarshalMethodName] [
	%struct.MarshalMethodName {
		i64 0, ; id 0x0; name: 
		ptr @.MarshalMethodName.0_name; char* name
	} ; 0
], align 8

; get_function_pointer (uint32_t mono_image_index, uint32_t class_index, uint32_t method_token, void*& target_ptr)
@get_function_pointer = internal dso_local unnamed_addr global ptr null, align 4

; Functions

; Function attributes: "min-legal-vector-width"="0" mustprogress nofree norecurse nosync "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" uwtable willreturn
define void @xamarin_app_init(ptr nocapture noundef readnone %env, ptr noundef %fn) local_unnamed_addr #0
{
	%fnIsNull = icmp eq ptr %fn, null
	br i1 %fnIsNull, label %1, label %2

1: ; preds = %0
	%putsResult = call noundef i32 @puts(ptr @.str.0)
	call void @abort()
	unreachable 

2: ; preds = %1, %0
	store ptr %fn, ptr @get_function_pointer, align 4, !tbaa !3
	ret void
}

; Strings
@.str.0 = private unnamed_addr constant [40 x i8] c"get_function_pointer MUST be specified\0A\00", align 1

;MarshalMethodName
@.MarshalMethodName.0_name = private unnamed_addr constant [1 x i8] c"\00", align 1

; External functions

; Function attributes: noreturn "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8"
declare void @abort() local_unnamed_addr #2

; Function attributes: nofree nounwind
declare noundef i32 @puts(ptr noundef) local_unnamed_addr #1
attributes #0 = { "min-legal-vector-width"="0" mustprogress nofree norecurse nosync "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" "stackrealign" "target-cpu"="i686" "target-features"="+cx8,+mmx,+sse,+sse2,+sse3,+ssse3,+x87" "tune-cpu"="generic" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { noreturn "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" "stackrealign" "target-cpu"="i686" "target-features"="+cx8,+mmx,+sse,+sse2,+sse3,+ssse3,+x87" "tune-cpu"="generic" }

; Metadata
!llvm.module.flags = !{!0, !1, !7}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!"Xamarin.Android remotes/origin/release/8.0.4xx @ 82d8938cf80f6d5fa6c28529ddfbdb753d805ab4"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"NumRegisterParameters", i32 0}
