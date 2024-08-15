using System;
using System.Runtime.InteropServices;
using System.Net.Http;
using TTSEngineLib;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SapiComClient;

#nullable enable

[Guid("97333761-a00a-4bf3-9b97-3061878b42bd")]
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public class Entrypoint : ISpTTSEngine, ISpObjectWithToken
{
    private ISpObjectToken? token;
    private readonly HttpClient httpClient = new();

    public void Speak(uint dwSpeakFlags, ref Guid rguidFormatId, ref WAVEFORMATEX pWaveFormatEx, ref SPVTEXTFRAG pTextFragList, ISpTTSEngineSite pOutputSite)
    {
        var currentTextList = pTextFragList;
        var tasks = new List<Task>();
        while (true)
        {
            var text = currentTextList.pTextStart;
            tasks.Add(InvokeTalk(text));

            if (currentTextList.pNext == IntPtr.Zero)
            {
                break;
            }

            currentTextList = Marshal.PtrToStructure<SPVTEXTFRAG>(currentTextList.pNext);
        }

        Task.WaitAll([.. tasks]);
    }

    private Task InvokeTalk(string text)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"text", text }
            });

        return httpClient.PostAsync("http://localhost:4532/talk", content);
    }

    public unsafe void GetOutputFormat(ref Guid pTargetFmtId, ref WAVEFORMATEX pTargetWaveFormatEx, out Guid pOutputFormatId, IntPtr ppCoMemOutputWaveFormatEx)
    {
        // https://github.com/vstojkovic/sapi-lite/blob/202e96fd1cca47863f5eca2c9b5b82b7ea390d88/src/audio/stream.rs#L24
        pOutputFormatId = Guid.Parse("c31adbae-527f-4ff5-a230-f62bb61ff70c");

        var format = new WAVEFORMATEX()
        {
            wFormatTag = 1,
            nChannels = 1,
            cbSize = 0,
            nSamplesPerSec = 24000,
            wBitsPerSample = 16,
            nBlockAlign = 2,
            nAvgBytesPerSec = 48000,
        };
        var intPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(format));
        Marshal.StructureToPtr(format, intPtr, false);

        WAVEFORMATEX** ppFormat = (WAVEFORMATEX**)ppCoMemOutputWaveFormatEx.ToPointer();
        *ppFormat = (WAVEFORMATEX*)intPtr.ToPointer();
    }

    public void GetObjectToken(out ISpObjectToken ppToken)
    {
        ppToken = token ?? throw new ApplicationException("token is null");
    }

    public void SetObjectToken(ISpObjectToken pToken)
    {
        token = pToken;
    }
}
