; ModuleID = 'marshal_methods.arm64-v8a.ll'
source_filename = "marshal_methods.arm64-v8a.ll"
target datalayout = "e-m:e-i8:8:32-i16:16:32-i64:64-i128:128-n32:64-S128"
target triple = "aarch64-unknown-linux-android21"

%struct.MarshalMethodName = type {
	i64, ; uint64_t id
	ptr ; char* name
}

%struct.MarshalMethodsManagedClass = type {
	i32, ; uint32_t token
	ptr ; MonoClass klass
}

@assembly_image_cache = dso_local local_unnamed_addr global [167 x ptr] zeroinitializer, align 8

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [334 x i64] [
	i64 4486185731092394, ; 0: Serilog.Sinks.RollingFile.dll => 0xff02982e5efaa => 69
	i64 98382396393917666, ; 1: Microsoft.Extensions.Primitives.dll => 0x15d8644ad360ce2 => 56
	i64 120698629574877762, ; 2: Mono.Android => 0x1accec39cafe242 => 166
	i64 131669012237370309, ; 3: Microsoft.Maui.Essentials.dll => 0x1d3c844de55c3c5 => 61
	i64 160518225272466977, ; 4: Microsoft.Extensions.Hosting.Abstractions => 0x23a4679b5576e21 => 51
	i64 196720943101637631, ; 5: System.Linq.Expressions.dll => 0x2bae4a7cd73f3ff => 124
	i64 210515253464952879, ; 6: Xamarin.AndroidX.Collection.dll => 0x2ebe681f694702f => 81
	i64 232391251801502327, ; 7: Xamarin.AndroidX.SavedState.dll => 0x3399e9cbc897277 => 98
	i64 435118502366263740, ; 8: Xamarin.AndroidX.Security.SecurityCrypto.dll => 0x609d9f8f8bdb9bc => 99
	i64 545109961164950392, ; 9: fi/Microsoft.Maui.Controls.resources.dll => 0x7909e9f1ec38b78 => 7
	i64 570522211579385009, ; 10: Serilog.dll => 0x7eae6edbda8d4b1 => 63
	i64 668723562677762733, ; 11: Microsoft.Extensions.Configuration.Binder.dll => 0x947c88986577aad => 47
	i64 750875890346172408, ; 12: System.Threading.Thread => 0xa6ba5a4da7d1ff8 => 155
	i64 799765834175365804, ; 13: System.ComponentModel.dll => 0xb1956c9f18442ac => 114
	i64 849051935479314978, ; 14: hi/Microsoft.Maui.Controls.resources.dll => 0xbc8703ca21a3a22 => 10
	i64 870603111519317375, ; 15: SQLitePCLRaw.lib.e_sqlite3.android => 0xc1500ead2756d7f => 73
	i64 872800313462103108, ; 16: Xamarin.AndroidX.DrawerLayout => 0xc1ccf42c3c21c44 => 86
	i64 1120440138749646132, ; 17: Xamarin.Google.Android.Material.dll => 0xf8c9a5eae431534 => 103
	i64 1121665720830085036, ; 18: nb/Microsoft.Maui.Controls.resources.dll => 0xf90f507becf47ac => 18
	i64 1301485588176585670, ; 19: SQLitePCLRaw.core => 0x120fce3f338e43c6 => 72
	i64 1369545283391376210, ; 20: Xamarin.AndroidX.Navigation.Fragment.dll => 0x13019a2dd85acb52 => 94
	i64 1476839205573959279, ; 21: System.Net.Primitives.dll => 0x147ec96ece9b1e6f => 131
	i64 1486715745332614827, ; 22: Microsoft.Maui.Controls.dll => 0x14a1e017ea87d6ab => 58
	i64 1513467482682125403, ; 23: Mono.Android.Runtime => 0x1500eaa8245f6c5b => 165
	i64 1518315023656898250, ; 24: SQLitePCLRaw.provider.e_sqlite3 => 0x151223783a354eca => 74
	i64 1537168428375924959, ; 25: System.Threading.Thread.dll => 0x15551e8a954ae0df => 155
	i64 1556147632182429976, ; 26: ko/Microsoft.Maui.Controls.resources.dll => 0x15988c06d24c8918 => 16
	i64 1569189741918430135, ; 27: Serilog.Extensions.Logging.File => 0x15c6e1c1a03c23b7 => 65
	i64 1624659445732251991, ; 28: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 0x168bf32877da9957 => 79
	i64 1628611045998245443, ; 29: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 0x1699fd1e1a00b643 => 91
	i64 1735388228521408345, ; 30: System.Net.Mail.dll => 0x181556663c69b759 => 128
	i64 1743969030606105336, ; 31: System.Memory.dll => 0x1833d297e88f2af8 => 126
	i64 1767386781656293639, ; 32: System.Private.Uri.dll => 0x188704e9f5582107 => 140
	i64 1795316252682057001, ; 33: Xamarin.AndroidX.AppCompat.dll => 0x18ea3e9eac997529 => 78
	i64 1825687700144851180, ; 34: System.Runtime.InteropServices.RuntimeInformation.dll => 0x1956254a55ef08ec => 143
	i64 1835311033149317475, ; 35: es\Microsoft.Maui.Controls.resources => 0x197855a927386163 => 6
	i64 1836611346387731153, ; 36: Xamarin.AndroidX.SavedState => 0x197cf449ebe482d1 => 98
	i64 1881198190668717030, ; 37: tr\Microsoft.Maui.Controls.resources => 0x1a1b5bc992ea9be6 => 28
	i64 1897575647115118287, ; 38: Xamarin.AndroidX.Security.SecurityCrypto => 0x1a558aff4cba86cf => 99
	i64 1920760634179481754, ; 39: Microsoft.Maui.Controls.Xaml => 0x1aa7e99ec2d2709a => 59
	i64 1930726298510463061, ; 40: CommunityToolkit.Mvvm.dll => 0x1acb5156cd389055 => 37
	i64 1959996714666907089, ; 41: tr/Microsoft.Maui.Controls.resources.dll => 0x1b334ea0a2a755d1 => 28
	i64 1981742497975770890, ; 42: Xamarin.AndroidX.Lifecycle.ViewModel.dll => 0x1b80904d5c241f0a => 90
	i64 1983698669889758782, ; 43: cs/Microsoft.Maui.Controls.resources.dll => 0x1b87836e2031a63e => 2
	i64 2019660174692588140, ; 44: pl/Microsoft.Maui.Controls.resources.dll => 0x1c07463a6f8e1a6c => 20
	i64 2200176636225660136, ; 45: Microsoft.Extensions.Logging.Debug.dll => 0x1e8898fe5d5824e8 => 54
	i64 2262844636196693701, ; 46: Xamarin.AndroidX.DrawerLayout.dll => 0x1f673d352266e6c5 => 86
	i64 2287834202362508563, ; 47: System.Collections.Concurrent => 0x1fc00515e8ce7513 => 107
	i64 2302323944321350744, ; 48: ru/Microsoft.Maui.Controls.resources.dll => 0x1ff37f6ddb267c58 => 24
	i64 2315304989185124968, ; 49: System.IO.FileSystem.dll => 0x20219d9ee311aa68 => 122
	i64 2329709569556905518, ; 50: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 0x2054ca829b447e2e => 89
	i64 2335503487726329082, ; 51: System.Text.Encodings.Web => 0x2069600c4d9d1cfa => 151
	i64 2470498323731680442, ; 52: Xamarin.AndroidX.CoordinatorLayout => 0x2248f922dc398cba => 82
	i64 2497223385847772520, ; 53: System.Runtime => 0x22a7eb7046413568 => 147
	i64 2547086958574651984, ; 54: Xamarin.AndroidX.Activity.dll => 0x2359121801df4a50 => 77
	i64 2602673633151553063, ; 55: th\Microsoft.Maui.Controls.resources => 0x241e8de13a460e27 => 27
	i64 2632269733008246987, ; 56: System.Net.NameResolution => 0x2487b36034f808cb => 129
	i64 2656907746661064104, ; 57: Microsoft.Extensions.DependencyInjection => 0x24df3b84c8b75da8 => 48
	i64 2662981627730767622, ; 58: cs\Microsoft.Maui.Controls.resources => 0x24f4cfae6c48af06 => 2
	i64 2706075432581334785, ; 59: System.Net.WebSockets => 0x258de944be6c0701 => 137
	i64 2895129759130297543, ; 60: fi\Microsoft.Maui.Controls.resources => 0x282d912d479fa4c7 => 7
	i64 3017704767998173186, ; 61: Xamarin.Google.Android.Material => 0x29e10a7f7d88a002 => 103
	i64 3168817962471953758, ; 62: Microsoft.Extensions.Hosting.Abstractions.dll => 0x2bf9e725d304955e => 51
	i64 3289520064315143713, ; 63: Xamarin.AndroidX.Lifecycle.Common => 0x2da6b911e3063621 => 88
	i64 3311221304742556517, ; 64: System.Numerics.Vectors.dll => 0x2df3d23ba9e2b365 => 138
	i64 3325875462027654285, ; 65: System.Runtime.Numerics => 0x2e27e21c8958b48d => 146
	i64 3328853167529574890, ; 66: System.Net.Sockets.dll => 0x2e327651a008c1ea => 134
	i64 3344514922410554693, ; 67: Xamarin.KotlinX.Coroutines.Core.Jvm => 0x2e6a1a9a18463545 => 105
	i64 3429672777697402584, ; 68: Microsoft.Maui.Essentials => 0x2f98a5385a7b1ed8 => 61
	i64 3494946837667399002, ; 69: Microsoft.Extensions.Configuration => 0x30808ba1c00a455a => 45
	i64 3522470458906976663, ; 70: Xamarin.AndroidX.SwipeRefreshLayout => 0x30e2543832f52197 => 100
	i64 3551103847008531295, ; 71: System.Private.CoreLib.dll => 0x31480e226177735f => 162
	i64 3567343442040498961, ; 72: pt\Microsoft.Maui.Controls.resources => 0x3181bff5bea4ab11 => 22
	i64 3571415421602489686, ; 73: System.Runtime.dll => 0x319037675df7e556 => 147
	i64 3638003163729360188, ; 74: Microsoft.Extensions.Configuration.Abstractions => 0x327cc89a39d5f53c => 46
	i64 3647754201059316852, ; 75: System.Xml.ReaderWriter => 0x329f6d1e86145474 => 159
	i64 3655542548057982301, ; 76: Microsoft.Extensions.Configuration.dll => 0x32bb18945e52855d => 45
	i64 3716579019761409177, ; 77: netstandard.dll => 0x3393f0ed5c8c5c99 => 161
	i64 3727469159507183293, ; 78: Xamarin.AndroidX.RecyclerView => 0x33baa1739ba646bd => 97
	i64 3783726507060260521, ; 79: Microsoft.AspNetCore.SignalR.Common.dll => 0x34827f360c8e6ea9 => 43
	i64 3847993472088771242, ; 80: Serilog.Extensions.Logging.File.dll => 0x3566d1ace1df8aaa => 65
	i64 3869221888984012293, ; 81: Microsoft.Extensions.Logging.dll => 0x35b23cceda0ed605 => 52
	i64 3890352374528606784, ; 82: Microsoft.Maui.Controls.Xaml.dll => 0x35fd4edf66e00240 => 59
	i64 3933965368022646939, ; 83: System.Net.Requests => 0x369840a8bfadc09b => 132
	i64 3966267475168208030, ; 84: System.Memory => 0x370b03412596249e => 126
	i64 4073500526318903918, ; 85: System.Private.Xml.dll => 0x3887fb25779ae26e => 141
	i64 4073631083018132676, ; 86: Microsoft.Maui.Controls.Compatibility.dll => 0x388871e311491cc4 => 57
	i64 4120493066591692148, ; 87: zh-Hant\Microsoft.Maui.Controls.resources => 0x392eee9cdda86574 => 33
	i64 4154383907710350974, ; 88: System.ComponentModel => 0x39a7562737acb67e => 114
	i64 4187479170553454871, ; 89: System.Linq.Expressions => 0x3a1cea1e912fa117 => 124
	i64 4205801962323029395, ; 90: System.ComponentModel.TypeConverter => 0x3a5e0299f7e7ad93 => 113
	i64 4337444564132831293, ; 91: SQLitePCLRaw.batteries_v2.dll => 0x3c31b2d9ae16203d => 71
	i64 4356591372459378815, ; 92: vi/Microsoft.Maui.Controls.resources.dll => 0x3c75b8c562f9087f => 30
	i64 4477672992252076438, ; 93: System.Web.HttpUtility.dll => 0x3e23e3dcdb8ba196 => 158
	i64 4679594760078841447, ; 94: ar/Microsoft.Maui.Controls.resources.dll => 0x40f142a407475667 => 0
	i64 4794310189461587505, ; 95: Xamarin.AndroidX.Activity => 0x4288cfb749e4c631 => 77
	i64 4795410492532947900, ; 96: Xamarin.AndroidX.SwipeRefreshLayout.dll => 0x428cb86f8f9b7bbc => 100
	i64 4814660307502931973, ; 97: System.Net.NameResolution.dll => 0x42d11c0a5ee2a005 => 129
	i64 4853321196694829351, ; 98: System.Runtime.Loader.dll => 0x435a75ea15de7927 => 145
	i64 5103417709280584325, ; 99: System.Collections.Specialized => 0x46d2fb5e161b6285 => 110
	i64 5182934613077526976, ; 100: System.Collections.Specialized.dll => 0x47ed7b91fa9009c0 => 110
	i64 5290786973231294105, ; 101: System.Runtime.Loader => 0x496ca6b869b72699 => 145
	i64 5423376490970181369, ; 102: System.Runtime.InteropServices.RuntimeInformation => 0x4b43b42f2b7b6ef9 => 143
	i64 5471532531798518949, ; 103: sv\Microsoft.Maui.Controls.resources => 0x4beec9d926d82ca5 => 26
	i64 5522859530602327440, ; 104: uk\Microsoft.Maui.Controls.resources => 0x4ca5237b51eead90 => 29
	i64 5527431512186326818, ; 105: System.IO.FileSystem.Primitives.dll => 0x4cb561acbc2a8f22 => 121
	i64 5570799893513421663, ; 106: System.IO.Compression.Brotli => 0x4d4f74fcdfa6c35f => 119
	i64 5573260873512690141, ; 107: System.Security.Cryptography.dll => 0x4d58333c6e4ea1dd => 148
	i64 5692067934154308417, ; 108: Xamarin.AndroidX.ViewPager2.dll => 0x4efe49a0d4a8bb41 => 102
	i64 5979151488806146654, ; 109: System.Formats.Asn1 => 0x52fa3699a489d25e => 117
	i64 6014447449592687183, ; 110: Microsoft.AspNetCore.Http.Connections.Common.dll => 0x53779c16e939ea4f => 40
	i64 6034224070161570862, ; 111: Microsoft.AspNetCore.SignalR.Client.dll => 0x53bdded235179c2e => 41
	i64 6068057819846744445, ; 112: ro/Microsoft.Maui.Controls.resources.dll => 0x5436126fec7f197d => 23
	i64 6183170893902868313, ; 113: SQLitePCLRaw.batteries_v2 => 0x55cf092b0c9d6f59 => 71
	i64 6200764641006662125, ; 114: ro\Microsoft.Maui.Controls.resources => 0x560d8a96830131ed => 23
	i64 6222399776351216807, ; 115: System.Text.Json.dll => 0x565a67a0ffe264a7 => 152
	i64 6357457916754632952, ; 116: _Microsoft.Android.Resource.Designer => 0x583a3a4ac2a7a0f8 => 34
	i64 6401687960814735282, ; 117: Xamarin.AndroidX.Lifecycle.LiveData.Core => 0x58d75d486341cfb2 => 89
	i64 6478287442656530074, ; 118: hr\Microsoft.Maui.Controls.resources => 0x59e7801b0c6a8e9a => 11
	i64 6548213210057960872, ; 119: Xamarin.AndroidX.CustomView.dll => 0x5adfed387b066da8 => 85
	i64 6560151584539558821, ; 120: Microsoft.Extensions.Options => 0x5b0a571be53243a5 => 55
	i64 6743165466166707109, ; 121: nl\Microsoft.Maui.Controls.resources => 0x5d948943c08c43a5 => 19
	i64 6777482997383978746, ; 122: pt/Microsoft.Maui.Controls.resources.dll => 0x5e0e74e0a2525efa => 22
	i64 6783125919820072783, ; 123: Microsoft.AspNetCore.Connections.Abstractions => 0x5e228115e59ec74f => 38
	i64 6786606130239981554, ; 124: System.Diagnostics.TraceSource => 0x5e2ede51877147f2 => 116
	i64 6894844156784520562, ; 125: System.Numerics.Vectors => 0x5faf683aead1ad72 => 138
	i64 7017588408768804231, ; 126: Microsoft.AspNetCore.SignalR.Protocols.Json => 0x61637b7a1c903587 => 44
	i64 7112547816752919026, ; 127: System.IO.FileSystem => 0x62b4d88e3189b1f2 => 122
	i64 7220009545223068405, ; 128: sv/Microsoft.Maui.Controls.resources.dll => 0x6432a06d99f35af5 => 26
	i64 7270811800166795866, ; 129: System.Linq => 0x64e71ccf51a90a5a => 125
	i64 7377312882064240630, ; 130: System.ComponentModel.TypeConverter.dll => 0x66617afac45a2ff6 => 113
	i64 7489048572193775167, ; 131: System.ObjectModel => 0x67ee71ff6b419e3f => 139
	i64 7633078081492531274, ; 132: Serilog.Sinks.Async.dll => 0x69ee2412c62eb84a => 67
	i64 7654504624184590948, ; 133: System.Net.Http => 0x6a3a4366801b8264 => 127
	i64 7694700312542370399, ; 134: System.Net.Mail => 0x6ac9112a7e2cda5f => 128
	i64 7708790323521193081, ; 135: ms/Microsoft.Maui.Controls.resources.dll => 0x6afb1ff4d1730479 => 17
	i64 7714652370974252055, ; 136: System.Private.CoreLib => 0x6b0ff375198b9c17 => 162
	i64 7735352534559001595, ; 137: Xamarin.Kotlin.StdLib.dll => 0x6b597e2582ce8bfb => 104
	i64 7836164640616011524, ; 138: Xamarin.AndroidX.AppCompat.AppCompatResources => 0x6cbfa6390d64d704 => 79
	i64 8064050204834738623, ; 139: System.Collections.dll => 0x6fe942efa61731bf => 111
	i64 8083354569033831015, ; 140: Xamarin.AndroidX.Lifecycle.Common.dll => 0x702dd82730cad267 => 88
	i64 8087206902342787202, ; 141: System.Diagnostics.DiagnosticSource => 0x703b87d46f3aa082 => 75
	i64 8167236081217502503, ; 142: Java.Interop.dll => 0x7157d9f1a9b8fd27 => 163
	i64 8185542183669246576, ; 143: System.Collections => 0x7198e33f4794aa70 => 111
	i64 8243855692487634729, ; 144: Microsoft.AspNetCore.SignalR.Protocols.Json.dll => 0x72680f13124eaf29 => 44
	i64 8246048515196606205, ; 145: Microsoft.Maui.Graphics.dll => 0x726fd96f64ee56fd => 62
	i64 8290740647658429042, ; 146: System.Runtime.Extensions => 0x730ea0b15c929a72 => 142
	i64 8368701292315763008, ; 147: System.Security.Cryptography => 0x7423997c6fd56140 => 148
	i64 8400357532724379117, ; 148: Xamarin.AndroidX.Navigation.UI.dll => 0x749410ab44503ded => 96
	i64 8518412311883997971, ; 149: System.Collections.Immutable => 0x76377add7c28e313 => 108
	i64 8563666267364444763, ; 150: System.Private.Uri => 0x76d841191140ca5b => 140
	i64 8599632406834268464, ; 151: CommunityToolkit.Maui => 0x7758081c784b4930 => 35
	i64 8614108721271900878, ; 152: pt-BR/Microsoft.Maui.Controls.resources.dll => 0x778b763e14018ace => 21
	i64 8626175481042262068, ; 153: Java.Interop => 0x77b654e585b55834 => 163
	i64 8639588376636138208, ; 154: Xamarin.AndroidX.Navigation.Runtime => 0x77e5fbdaa2fda2e0 => 95
	i64 8677882282824630478, ; 155: pt-BR\Microsoft.Maui.Controls.resources => 0x786e07f5766b00ce => 21
	i64 8725526185868997716, ; 156: System.Diagnostics.DiagnosticSource.dll => 0x79174bd613173454 => 75
	i64 9045785047181495996, ; 157: zh-HK\Microsoft.Maui.Controls.resources => 0x7d891592e3cb0ebc => 31
	i64 9312692141327339315, ; 158: Xamarin.AndroidX.ViewPager2 => 0x813d54296a634f33 => 102
	i64 9324707631942237306, ; 159: Xamarin.AndroidX.AppCompat => 0x8168042fd44a7c7a => 78
	i64 9584643793929893533, ; 160: System.IO.dll => 0x85037ebfbbd7f69d => 123
	i64 9659729154652888475, ; 161: System.Text.RegularExpressions => 0x860e407c9991dd9b => 153
	i64 9678050649315576968, ; 162: Xamarin.AndroidX.CoordinatorLayout.dll => 0x864f57c9feb18c88 => 82
	i64 9702891218465930390, ; 163: System.Collections.NonGeneric.dll => 0x86a79827b2eb3c96 => 109
	i64 9737654085557355179, ; 164: Serilog => 0x872318cc6b4702ab => 63
	i64 9808709177481450983, ; 165: Mono.Android.dll => 0x881f890734e555e7 => 166
	i64 9892061368544164759, ; 166: Serilog.Formatting.Compact.dll => 0x8947a96780707b97 => 66
	i64 9926151752036674810, ; 167: Serilog.Extensions.Logging.dll => 0x89c0c66d6ec46cfa => 64
	i64 9956195530459977388, ; 168: Microsoft.Maui => 0x8a2b8315b36616ac => 60
	i64 9991543690424095600, ; 169: es/Microsoft.Maui.Controls.resources.dll => 0x8aa9180c89861370 => 6
	i64 10017511394021241210, ; 170: Microsoft.Extensions.Logging.Debug => 0x8b055989ae10717a => 54
	i64 10038780035334861115, ; 171: System.Net.Http.dll => 0x8b50e941206af13b => 127
	i64 10051358222726253779, ; 172: System.Private.Xml => 0x8b7d990c97ccccd3 => 141
	i64 10078727084704864206, ; 173: System.Net.WebSockets.Client => 0x8bded4e257f117ce => 136
	i64 10092835686693276772, ; 174: Microsoft.Maui.Controls => 0x8c10f49539bd0c64 => 58
	i64 10143853363526200146, ; 175: da\Microsoft.Maui.Controls.resources => 0x8cc634e3c2a16b52 => 3
	i64 10205853378024263619, ; 176: Microsoft.Extensions.Configuration.Binder => 0x8da279930adb4fc3 => 47
	i64 10226498071391929720, ; 177: Microsoft.Extensions.Features => 0x8debd1d049888578 => 50
	i64 10229024438826829339, ; 178: Xamarin.AndroidX.CustomView => 0x8df4cb880b10061b => 85
	i64 10360651442923773544, ; 179: System.Text.Encoding => 0x8fc86d98211c1e68 => 150
	i64 10406448008575299332, ; 180: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 0x906b2153fcb3af04 => 105
	i64 10430153318873392755, ; 181: Xamarin.AndroidX.Core => 0x90bf592ea44f6673 => 83
	i64 10471191266809575759, ; 182: Hubbly.Mobile.dll => 0x915124fa795b794f => 106
	i64 10506226065143327199, ; 183: ca\Microsoft.Maui.Controls.resources => 0x91cd9cf11ed169df => 1
	i64 10566960649245365243, ; 184: System.Globalization.dll => 0x92a562b96dcd13fb => 118
	i64 10714184849103829812, ; 185: System.Runtime.Extensions.dll => 0x94b06e5aa4b4bb34 => 142
	i64 10785150219063592792, ; 186: System.Net.Primitives => 0x95ac8cfb68830758 => 131
	i64 10880838204485145808, ; 187: CommunityToolkit.Maui.dll => 0x970080b2a4d614d0 => 35
	i64 11002576679268595294, ; 188: Microsoft.Extensions.Logging.Abstractions => 0x98b1013215cd365e => 53
	i64 11009005086950030778, ; 189: Microsoft.Maui.dll => 0x98c7d7cc621ffdba => 60
	i64 11103970607964515343, ; 190: hu\Microsoft.Maui.Controls.resources => 0x9a193a6fc41a6c0f => 12
	i64 11162124722117608902, ; 191: Xamarin.AndroidX.ViewPager => 0x9ae7d54b986d05c6 => 101
	i64 11220793807500858938, ; 192: ja\Microsoft.Maui.Controls.resources => 0x9bb8448481fdd63a => 15
	i64 11226290749488709958, ; 193: Microsoft.Extensions.Options.dll => 0x9bcbcbf50c874146 => 55
	i64 11329751333533450475, ; 194: System.Threading.Timer.dll => 0x9d3b5ccf6cc500eb => 156
	i64 11340910727871153756, ; 195: Xamarin.AndroidX.CursorAdapter => 0x9d630238642d465c => 84
	i64 11446671985764974897, ; 196: Mono.Android.Export => 0x9edabf8623efc131 => 164
	i64 11485890710487134646, ; 197: System.Runtime.InteropServices => 0x9f6614bf0f8b71b6 => 144
	i64 11513602507638267977, ; 198: System.IO.Pipelines.dll => 0x9fc8887aa0d36049 => 76
	i64 11518296021396496455, ; 199: id\Microsoft.Maui.Controls.resources => 0x9fd9353475222047 => 13
	i64 11529969570048099689, ; 200: Xamarin.AndroidX.ViewPager.dll => 0xa002ae3c4dc7c569 => 101
	i64 11530571088791430846, ; 201: Microsoft.Extensions.Logging => 0xa004d1504ccd66be => 52
	i64 11597940890313164233, ; 202: netstandard => 0xa0f429ca8d1805c9 => 161
	i64 11705530742807338875, ; 203: he/Microsoft.Maui.Controls.resources.dll => 0xa272663128721f7b => 9
	i64 11739066727115742305, ; 204: SQLite-net.dll => 0xa2e98afdf8575c61 => 70
	i64 11806260347154423189, ; 205: SQLite-net => 0xa3d8433bc5eb5d95 => 70
	i64 12025046348429218519, ; 206: Hubbly.Mobile => 0xa6e18bf145bb52d7 => 106
	i64 12145679461940342714, ; 207: System.Text.Json => 0xa88e1f1ebcb62fba => 152
	i64 12269460666702402136, ; 208: System.Collections.Immutable.dll => 0xaa45e178506c9258 => 108
	i64 12279246230491828964, ; 209: SQLitePCLRaw.provider.e_sqlite3.dll => 0xaa68a5636e0512e4 => 74
	i64 12313367145828839434, ; 210: System.IO.Pipelines => 0xaae1de2e1c17f00a => 76
	i64 12341818387765915815, ; 211: CommunityToolkit.Maui.Core.dll => 0xab46f26f152bf0a7 => 36
	i64 12451044538927396471, ; 212: Xamarin.AndroidX.Fragment.dll => 0xaccaff0a2955b677 => 87
	i64 12466513435562512481, ; 213: Xamarin.AndroidX.Loader.dll => 0xad01f3eb52569061 => 92
	i64 12475113361194491050, ; 214: _Microsoft.Android.Resource.Designer.dll => 0xad2081818aba1caa => 34
	i64 12517810545449516888, ; 215: System.Diagnostics.TraceSource.dll => 0xadb8325e6f283f58 => 116
	i64 12538491095302438457, ; 216: Xamarin.AndroidX.CardView.dll => 0xae01ab382ae67e39 => 80
	i64 12550732019250633519, ; 217: System.IO.Compression => 0xae2d28465e8e1b2f => 120
	i64 12681088699309157496, ; 218: it/Microsoft.Maui.Controls.resources.dll => 0xaffc46fc178aec78 => 14
	i64 12700543734426720211, ; 219: Xamarin.AndroidX.Collection => 0xb041653c70d157d3 => 81
	i64 12708238894395270091, ; 220: System.IO => 0xb05cbbf17d3ba3cb => 123
	i64 12708922737231849740, ; 221: System.Text.Encoding.Extensions => 0xb05f29e50e96e90c => 149
	i64 12823819093633476069, ; 222: th/Microsoft.Maui.Controls.resources.dll => 0xb1f75b85abe525e5 => 27
	i64 12843321153144804894, ; 223: Microsoft.Extensions.Primitives => 0xb23ca48abd74d61e => 56
	i64 12859557719246324186, ; 224: System.Net.WebHeaderCollection.dll => 0xb276539ce04f41da => 135
	i64 13221551921002590604, ; 225: ca/Microsoft.Maui.Controls.resources.dll => 0xb77c636bdebe318c => 1
	i64 13222659110913276082, ; 226: ja/Microsoft.Maui.Controls.resources.dll => 0xb78052679c1178b2 => 15
	i64 13295219713260136977, ; 227: Microsoft.AspNetCore.Http.Connections.Client => 0xb8821be35ba42a11 => 39
	i64 13343850469010654401, ; 228: Mono.Android.Runtime.dll => 0xb92ee14d854f44c1 => 165
	i64 13381594904270902445, ; 229: he\Microsoft.Maui.Controls.resources => 0xb9b4f9aaad3e94ad => 9
	i64 13428779960367410341, ; 230: Microsoft.AspNetCore.SignalR.Client.Core.dll => 0xba5c9c39a8956ca5 => 42
	i64 13465488254036897740, ; 231: Xamarin.Kotlin.StdLib => 0xbadf06394d106fcc => 104
	i64 13467053111158216594, ; 232: uk/Microsoft.Maui.Controls.resources.dll => 0xbae49573fde79792 => 29
	i64 13540124433173649601, ; 233: vi\Microsoft.Maui.Controls.resources => 0xbbe82f6eede718c1 => 30
	i64 13545416393490209236, ; 234: id/Microsoft.Maui.Controls.resources.dll => 0xbbfafc7174bc99d4 => 13
	i64 13572454107664307259, ; 235: Xamarin.AndroidX.RecyclerView.dll => 0xbc5b0b19d99f543b => 97
	i64 13717397318615465333, ; 236: System.ComponentModel.Primitives.dll => 0xbe5dfc2ef2f87d75 => 112
	i64 13755568601956062840, ; 237: fr/Microsoft.Maui.Controls.resources.dll => 0xbee598c36b1b9678 => 8
	i64 13814445057219246765, ; 238: hr/Microsoft.Maui.Controls.resources.dll => 0xbfb6c49664b43aad => 11
	i64 13881769479078963060, ; 239: System.Console.dll => 0xc0a5f3cade5c6774 => 115
	i64 13959074834287824816, ; 240: Xamarin.AndroidX.Fragment => 0xc1b8989a7ad20fb0 => 87
	i64 14100563506285742564, ; 241: da/Microsoft.Maui.Controls.resources.dll => 0xc3af43cd0cff89e4 => 3
	i64 14124974489674258913, ; 242: Xamarin.AndroidX.CardView => 0xc405fd76067d19e1 => 80
	i64 14125464355221830302, ; 243: System.Threading.dll => 0xc407bafdbc707a9e => 157
	i64 14254574811015963973, ; 244: System.Text.Encoding.Extensions.dll => 0xc5d26c4442d66545 => 149
	i64 14461014870687870182, ; 245: System.Net.Requests.dll => 0xc8afd8683afdece6 => 132
	i64 14464374589798375073, ; 246: ru\Microsoft.Maui.Controls.resources => 0xc8bbc80dcb1e5ea1 => 24
	i64 14522721392235705434, ; 247: el/Microsoft.Maui.Controls.resources.dll => 0xc98b12295c2cf45a => 5
	i64 14551742072151931844, ; 248: System.Text.Encodings.Web.dll => 0xc9f22c50f1b8fbc4 => 151
	i64 14556034074661724008, ; 249: CommunityToolkit.Maui.Core => 0xca016bdea6b19f68 => 36
	i64 14604329626201521481, ; 250: Microsoft.AspNetCore.SignalR.Client => 0xcaad006b00747d49 => 41
	i64 14669215534098758659, ; 251: Microsoft.Extensions.DependencyInjection.dll => 0xcb9385ceb3993c03 => 48
	i64 14690985099581930927, ; 252: System.Web.HttpUtility => 0xcbe0dd1ca5233daf => 158
	i64 14705122255218365489, ; 253: ko\Microsoft.Maui.Controls.resources => 0xcc1316c7b0fb5431 => 16
	i64 14744092281598614090, ; 254: zh-Hans\Microsoft.Maui.Controls.resources => 0xcc9d89d004439a4a => 32
	i64 14809184851036126845, ; 255: Microsoft.AspNetCore.SignalR.Client.Core => 0xcd84cb28db1abe7d => 42
	i64 14822143737991655809, ; 256: Serilog.Extensions.Logging => 0xcdb2d532d8c5f181 => 64
	i64 14852515768018889994, ; 257: Xamarin.AndroidX.CursorAdapter.dll => 0xce1ebc6625a76d0a => 84
	i64 14892012299694389861, ; 258: zh-Hant/Microsoft.Maui.Controls.resources.dll => 0xceab0e490a083a65 => 33
	i64 14904040806490515477, ; 259: ar\Microsoft.Maui.Controls.resources => 0xced5ca2604cb2815 => 0
	i64 14954917835170835695, ; 260: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 0xcf8a8a895a82ecef => 49
	i64 14984936317414011727, ; 261: System.Net.WebHeaderCollection => 0xcff5302fe54ff34f => 135
	i64 14987728460634540364, ; 262: System.IO.Compression.dll => 0xcfff1ba06622494c => 120
	i64 15015154896917945444, ; 263: System.Net.Security.dll => 0xd0608bd33642dc64 => 133
	i64 15076659072870671916, ; 264: System.ObjectModel.dll => 0xd13b0d8c1620662c => 139
	i64 15111608613780139878, ; 265: ms\Microsoft.Maui.Controls.resources => 0xd1b737f831192f66 => 17
	i64 15115185479366240210, ; 266: System.IO.Compression.Brotli.dll => 0xd1c3ed1c1bc467d2 => 119
	i64 15133485256822086103, ; 267: System.Linq.dll => 0xd204f0a9127dd9d7 => 125
	i64 15227001540531775957, ; 268: Microsoft.Extensions.Configuration.Abstractions.dll => 0xd3512d3999b8e9d5 => 46
	i64 15370334346939861994, ; 269: Xamarin.AndroidX.Core.dll => 0xd54e65a72c560bea => 83
	i64 15391712275433856905, ; 270: Microsoft.Extensions.DependencyInjection.Abstractions => 0xd59a58c406411f89 => 49
	i64 15526743539506359484, ; 271: System.Text.Encoding.dll => 0xd77a12fc26de2cbc => 150
	i64 15527772828719725935, ; 272: System.Console => 0xd77dbb1e38cd3d6f => 115
	i64 15536481058354060254, ; 273: de\Microsoft.Maui.Controls.resources => 0xd79cab34eec75bde => 4
	i64 15557562860424774966, ; 274: System.Net.Sockets => 0xd7e790fe7a6dc536 => 134
	i64 15582737692548360875, ; 275: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 0xd841015ed86f6aab => 91
	i64 15609085926864131306, ; 276: System.dll => 0xd89e9cf3334914ea => 160
	i64 15661133872274321916, ; 277: System.Xml.ReaderWriter.dll => 0xd9578647d4bfb1fc => 159
	i64 15664356999916475676, ; 278: de/Microsoft.Maui.Controls.resources.dll => 0xd962f9b2b6ecd51c => 4
	i64 15728474988603145424, ; 279: Serilog.Formatting.Compact => 0xda46c4ab4a4e7cd0 => 66
	i64 15743187114543869802, ; 280: hu/Microsoft.Maui.Controls.resources.dll => 0xda7b09450ae4ef6a => 12
	i64 15755368083429170162, ; 281: System.IO.FileSystem.Primitives => 0xdaa64fcbde529bf2 => 121
	i64 15783653065526199428, ; 282: el\Microsoft.Maui.Controls.resources => 0xdb0accd674b1c484 => 5
	i64 15847085070278954535, ; 283: System.Threading.Channels.dll => 0xdbec27e8f35f8e27 => 154
	i64 15928521404965645318, ; 284: Microsoft.Maui.Controls.Compatibility => 0xdd0d79d32c2eec06 => 57
	i64 15938041220918174886, ; 285: Serilog.Sinks.RollingFile => 0xdd2f4c0c0c5c64a6 => 69
	i64 16018552496348375205, ; 286: System.Net.NetworkInformation.dll => 0xde4d54a020caa8a5 => 130
	i64 16154507427712707110, ; 287: System => 0xe03056ea4e39aa26 => 160
	i64 16156430004425724367, ; 288: Microsoft.AspNetCore.Http.Connections.Client.dll => 0xe0372b7d144211cf => 39
	i64 16219561732052121626, ; 289: System.Net.Security => 0xe1177575db7c781a => 133
	i64 16288847719894691167, ; 290: nb\Microsoft.Maui.Controls.resources => 0xe20d9cb300c12d5f => 18
	i64 16321164108206115771, ; 291: Microsoft.Extensions.Logging.Abstractions.dll => 0xe2806c487e7b0bbb => 53
	i64 16343918515847859304, ; 292: Microsoft.Extensions.Features.dll => 0xe2d1434bdf0a8c68 => 50
	i64 16454459195343277943, ; 293: System.Net.NetworkInformation => 0xe459fb756d988f77 => 130
	i64 16496768397145114574, ; 294: Mono.Android.Export.dll => 0xe4f04b741db987ce => 164
	i64 16605226748660468415, ; 295: Microsoft.AspNetCore.SignalR.Common => 0xe6719dbfe8b8cabf => 43
	i64 16648892297579399389, ; 296: CommunityToolkit.Mvvm => 0xe70cbf55c4f508dd => 37
	i64 16649148416072044166, ; 297: Microsoft.Maui.Graphics => 0xe70da84600bb4e86 => 62
	i64 16669448769200862260, ; 298: Serilog.Sinks.File => 0xe755c75649ca3434 => 68
	i64 16677317093839702854, ; 299: Xamarin.AndroidX.Navigation.UI => 0xe771bb8960dd8b46 => 96
	i64 16755018182064898362, ; 300: SQLitePCLRaw.core.dll => 0xe885c843c330813a => 72
	i64 16890310621557459193, ; 301: System.Text.RegularExpressions.dll => 0xea66700587f088f9 => 153
	i64 16942731696432749159, ; 302: sk\Microsoft.Maui.Controls.resources => 0xeb20acb622a01a67 => 25
	i64 16998075588627545693, ; 303: Xamarin.AndroidX.Navigation.Fragment => 0xebe54bb02d623e5d => 94
	i64 17008137082415910100, ; 304: System.Collections.NonGeneric => 0xec090a90408c8cd4 => 109
	i64 17031351772568316411, ; 305: Xamarin.AndroidX.Navigation.Common.dll => 0xec5b843380a769fb => 93
	i64 17062143951396181894, ; 306: System.ComponentModel.Primitives => 0xecc8e986518c9786 => 112
	i64 17089008752050867324, ; 307: zh-Hans/Microsoft.Maui.Controls.resources.dll => 0xed285aeb25888c7c => 32
	i64 17118171214553292978, ; 308: System.Threading.Channels => 0xed8ff6060fc420b2 => 154
	i64 17338386382517543202, ; 309: System.Net.WebSockets.Client.dll => 0xf09e528d5c6da122 => 136
	i64 17342750010158924305, ; 310: hi\Microsoft.Maui.Controls.resources => 0xf0add33f97ecc211 => 10
	i64 17438153253682247751, ; 311: sk/Microsoft.Maui.Controls.resources.dll => 0xf200c3fe308d7847 => 25
	i64 17470386307322966175, ; 312: System.Threading.Timer => 0xf27347c8d0d5709f => 156
	i64 17504803799422154384, ; 313: Serilog.Sinks.File.dll => 0xf2ed8e4fa7773690 => 68
	i64 17509662556995089465, ; 314: System.Net.WebSockets.dll => 0xf2fed1534ea67439 => 137
	i64 17514990004910432069, ; 315: fr\Microsoft.Maui.Controls.resources => 0xf311be9c6f341f45 => 8
	i64 17571845317586269034, ; 316: Microsoft.AspNetCore.Connections.Abstractions.dll => 0xf3dbbc377ad7336a => 38
	i64 17623389608345532001, ; 317: pl\Microsoft.Maui.Controls.resources => 0xf492db79dfbef661 => 20
	i64 17627500474728259406, ; 318: System.Globalization => 0xf4a176498a351f4e => 118
	i64 17636563193350668017, ; 319: Microsoft.AspNetCore.Http.Connections.Common => 0xf4c1a8c826653ef1 => 40
	i64 17702523067201099846, ; 320: zh-HK/Microsoft.Maui.Controls.resources.dll => 0xf5abfef008ae1846 => 31
	i64 17704177640604968747, ; 321: Xamarin.AndroidX.Loader => 0xf5b1dfc36cac272b => 92
	i64 17710060891934109755, ; 322: Xamarin.AndroidX.Lifecycle.ViewModel => 0xf5c6c68c9e45303b => 90
	i64 17712670374920797664, ; 323: System.Runtime.InteropServices.dll => 0xf5d00bdc38bd3de0 => 144
	i64 17777860260071588075, ; 324: System.Runtime.Numerics.dll => 0xf6b7a5b72419c0eb => 146
	i64 18025913125965088385, ; 325: System.Threading => 0xfa28e87b91334681 => 157
	i64 18099568558057551825, ; 326: nl/Microsoft.Maui.Controls.resources.dll => 0xfb2e95b53ad977d1 => 19
	i64 18121036031235206392, ; 327: Xamarin.AndroidX.Navigation.Common => 0xfb7ada42d3d42cf8 => 93
	i64 18146411883821974900, ; 328: System.Formats.Asn1.dll => 0xfbd50176eb22c574 => 117
	i64 18245806341561545090, ; 329: System.Collections.Concurrent.dll => 0xfd3620327d587182 => 107
	i64 18305135509493619199, ; 330: Xamarin.AndroidX.Navigation.Runtime.dll => 0xfe08e7c2d8c199ff => 95
	i64 18324163916253801303, ; 331: it\Microsoft.Maui.Controls.resources => 0xfe4c81ff0a56ab57 => 14
	i64 18354827746502000950, ; 332: Serilog.Sinks.Async => 0xfeb972965fbc4136 => 67
	i64 18370042311372477656 ; 333: SQLitePCLRaw.lib.e_sqlite3.android.dll => 0xfeef80274e4094d8 => 73
], align 8

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [334 x i32] [
	i32 69, ; 0
	i32 56, ; 1
	i32 166, ; 2
	i32 61, ; 3
	i32 51, ; 4
	i32 124, ; 5
	i32 81, ; 6
	i32 98, ; 7
	i32 99, ; 8
	i32 7, ; 9
	i32 63, ; 10
	i32 47, ; 11
	i32 155, ; 12
	i32 114, ; 13
	i32 10, ; 14
	i32 73, ; 15
	i32 86, ; 16
	i32 103, ; 17
	i32 18, ; 18
	i32 72, ; 19
	i32 94, ; 20
	i32 131, ; 21
	i32 58, ; 22
	i32 165, ; 23
	i32 74, ; 24
	i32 155, ; 25
	i32 16, ; 26
	i32 65, ; 27
	i32 79, ; 28
	i32 91, ; 29
	i32 128, ; 30
	i32 126, ; 31
	i32 140, ; 32
	i32 78, ; 33
	i32 143, ; 34
	i32 6, ; 35
	i32 98, ; 36
	i32 28, ; 37
	i32 99, ; 38
	i32 59, ; 39
	i32 37, ; 40
	i32 28, ; 41
	i32 90, ; 42
	i32 2, ; 43
	i32 20, ; 44
	i32 54, ; 45
	i32 86, ; 46
	i32 107, ; 47
	i32 24, ; 48
	i32 122, ; 49
	i32 89, ; 50
	i32 151, ; 51
	i32 82, ; 52
	i32 147, ; 53
	i32 77, ; 54
	i32 27, ; 55
	i32 129, ; 56
	i32 48, ; 57
	i32 2, ; 58
	i32 137, ; 59
	i32 7, ; 60
	i32 103, ; 61
	i32 51, ; 62
	i32 88, ; 63
	i32 138, ; 64
	i32 146, ; 65
	i32 134, ; 66
	i32 105, ; 67
	i32 61, ; 68
	i32 45, ; 69
	i32 100, ; 70
	i32 162, ; 71
	i32 22, ; 72
	i32 147, ; 73
	i32 46, ; 74
	i32 159, ; 75
	i32 45, ; 76
	i32 161, ; 77
	i32 97, ; 78
	i32 43, ; 79
	i32 65, ; 80
	i32 52, ; 81
	i32 59, ; 82
	i32 132, ; 83
	i32 126, ; 84
	i32 141, ; 85
	i32 57, ; 86
	i32 33, ; 87
	i32 114, ; 88
	i32 124, ; 89
	i32 113, ; 90
	i32 71, ; 91
	i32 30, ; 92
	i32 158, ; 93
	i32 0, ; 94
	i32 77, ; 95
	i32 100, ; 96
	i32 129, ; 97
	i32 145, ; 98
	i32 110, ; 99
	i32 110, ; 100
	i32 145, ; 101
	i32 143, ; 102
	i32 26, ; 103
	i32 29, ; 104
	i32 121, ; 105
	i32 119, ; 106
	i32 148, ; 107
	i32 102, ; 108
	i32 117, ; 109
	i32 40, ; 110
	i32 41, ; 111
	i32 23, ; 112
	i32 71, ; 113
	i32 23, ; 114
	i32 152, ; 115
	i32 34, ; 116
	i32 89, ; 117
	i32 11, ; 118
	i32 85, ; 119
	i32 55, ; 120
	i32 19, ; 121
	i32 22, ; 122
	i32 38, ; 123
	i32 116, ; 124
	i32 138, ; 125
	i32 44, ; 126
	i32 122, ; 127
	i32 26, ; 128
	i32 125, ; 129
	i32 113, ; 130
	i32 139, ; 131
	i32 67, ; 132
	i32 127, ; 133
	i32 128, ; 134
	i32 17, ; 135
	i32 162, ; 136
	i32 104, ; 137
	i32 79, ; 138
	i32 111, ; 139
	i32 88, ; 140
	i32 75, ; 141
	i32 163, ; 142
	i32 111, ; 143
	i32 44, ; 144
	i32 62, ; 145
	i32 142, ; 146
	i32 148, ; 147
	i32 96, ; 148
	i32 108, ; 149
	i32 140, ; 150
	i32 35, ; 151
	i32 21, ; 152
	i32 163, ; 153
	i32 95, ; 154
	i32 21, ; 155
	i32 75, ; 156
	i32 31, ; 157
	i32 102, ; 158
	i32 78, ; 159
	i32 123, ; 160
	i32 153, ; 161
	i32 82, ; 162
	i32 109, ; 163
	i32 63, ; 164
	i32 166, ; 165
	i32 66, ; 166
	i32 64, ; 167
	i32 60, ; 168
	i32 6, ; 169
	i32 54, ; 170
	i32 127, ; 171
	i32 141, ; 172
	i32 136, ; 173
	i32 58, ; 174
	i32 3, ; 175
	i32 47, ; 176
	i32 50, ; 177
	i32 85, ; 178
	i32 150, ; 179
	i32 105, ; 180
	i32 83, ; 181
	i32 106, ; 182
	i32 1, ; 183
	i32 118, ; 184
	i32 142, ; 185
	i32 131, ; 186
	i32 35, ; 187
	i32 53, ; 188
	i32 60, ; 189
	i32 12, ; 190
	i32 101, ; 191
	i32 15, ; 192
	i32 55, ; 193
	i32 156, ; 194
	i32 84, ; 195
	i32 164, ; 196
	i32 144, ; 197
	i32 76, ; 198
	i32 13, ; 199
	i32 101, ; 200
	i32 52, ; 201
	i32 161, ; 202
	i32 9, ; 203
	i32 70, ; 204
	i32 70, ; 205
	i32 106, ; 206
	i32 152, ; 207
	i32 108, ; 208
	i32 74, ; 209
	i32 76, ; 210
	i32 36, ; 211
	i32 87, ; 212
	i32 92, ; 213
	i32 34, ; 214
	i32 116, ; 215
	i32 80, ; 216
	i32 120, ; 217
	i32 14, ; 218
	i32 81, ; 219
	i32 123, ; 220
	i32 149, ; 221
	i32 27, ; 222
	i32 56, ; 223
	i32 135, ; 224
	i32 1, ; 225
	i32 15, ; 226
	i32 39, ; 227
	i32 165, ; 228
	i32 9, ; 229
	i32 42, ; 230
	i32 104, ; 231
	i32 29, ; 232
	i32 30, ; 233
	i32 13, ; 234
	i32 97, ; 235
	i32 112, ; 236
	i32 8, ; 237
	i32 11, ; 238
	i32 115, ; 239
	i32 87, ; 240
	i32 3, ; 241
	i32 80, ; 242
	i32 157, ; 243
	i32 149, ; 244
	i32 132, ; 245
	i32 24, ; 246
	i32 5, ; 247
	i32 151, ; 248
	i32 36, ; 249
	i32 41, ; 250
	i32 48, ; 251
	i32 158, ; 252
	i32 16, ; 253
	i32 32, ; 254
	i32 42, ; 255
	i32 64, ; 256
	i32 84, ; 257
	i32 33, ; 258
	i32 0, ; 259
	i32 49, ; 260
	i32 135, ; 261
	i32 120, ; 262
	i32 133, ; 263
	i32 139, ; 264
	i32 17, ; 265
	i32 119, ; 266
	i32 125, ; 267
	i32 46, ; 268
	i32 83, ; 269
	i32 49, ; 270
	i32 150, ; 271
	i32 115, ; 272
	i32 4, ; 273
	i32 134, ; 274
	i32 91, ; 275
	i32 160, ; 276
	i32 159, ; 277
	i32 4, ; 278
	i32 66, ; 279
	i32 12, ; 280
	i32 121, ; 281
	i32 5, ; 282
	i32 154, ; 283
	i32 57, ; 284
	i32 69, ; 285
	i32 130, ; 286
	i32 160, ; 287
	i32 39, ; 288
	i32 133, ; 289
	i32 18, ; 290
	i32 53, ; 291
	i32 50, ; 292
	i32 130, ; 293
	i32 164, ; 294
	i32 43, ; 295
	i32 37, ; 296
	i32 62, ; 297
	i32 68, ; 298
	i32 96, ; 299
	i32 72, ; 300
	i32 153, ; 301
	i32 25, ; 302
	i32 94, ; 303
	i32 109, ; 304
	i32 93, ; 305
	i32 112, ; 306
	i32 32, ; 307
	i32 154, ; 308
	i32 136, ; 309
	i32 10, ; 310
	i32 25, ; 311
	i32 156, ; 312
	i32 68, ; 313
	i32 137, ; 314
	i32 8, ; 315
	i32 38, ; 316
	i32 20, ; 317
	i32 118, ; 318
	i32 40, ; 319
	i32 31, ; 320
	i32 92, ; 321
	i32 90, ; 322
	i32 144, ; 323
	i32 146, ; 324
	i32 157, ; 325
	i32 19, ; 326
	i32 93, ; 327
	i32 117, ; 328
	i32 107, ; 329
	i32 95, ; 330
	i32 14, ; 331
	i32 67, ; 332
	i32 73 ; 333
], align 4

@marshal_methods_number_of_classes = dso_local local_unnamed_addr constant i32 0, align 4

@marshal_methods_class_cache = dso_local local_unnamed_addr global [0 x %struct.MarshalMethodsManagedClass] zeroinitializer, align 8

; Names of classes in which marshal methods reside
@mm_class_names = dso_local local_unnamed_addr constant [0 x ptr] zeroinitializer, align 8

@mm_method_names = dso_local local_unnamed_addr constant [1 x %struct.MarshalMethodName] [
	%struct.MarshalMethodName {
		i64 0, ; id 0x0; name: 
		ptr @.MarshalMethodName.0_name; char* name
	} ; 0
], align 8

; get_function_pointer (uint32_t mono_image_index, uint32_t class_index, uint32_t method_token, void*& target_ptr)
@get_function_pointer = internal dso_local unnamed_addr global ptr null, align 8

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
	store ptr %fn, ptr @get_function_pointer, align 8, !tbaa !3
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
attributes #0 = { "min-legal-vector-width"="0" mustprogress nofree norecurse nosync "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+fix-cortex-a53-835769,+neon,+outline-atomics,+v8a" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { noreturn "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+fix-cortex-a53-835769,+neon,+outline-atomics,+v8a" }

; Metadata
!llvm.module.flags = !{!0, !1, !7, !8, !9, !10}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!"Xamarin.Android remotes/origin/release/8.0.4xx @ 82d8938cf80f6d5fa6c28529ddfbdb753d805ab4"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"branch-target-enforcement", i32 0}
!8 = !{i32 1, !"sign-return-address", i32 0}
!9 = !{i32 1, !"sign-return-address-all", i32 0}
!10 = !{i32 1, !"sign-return-address-with-bkey", i32 0}
