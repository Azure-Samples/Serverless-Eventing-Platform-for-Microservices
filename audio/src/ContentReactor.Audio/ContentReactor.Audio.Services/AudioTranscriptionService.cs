using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ContentReactor.Audio.Services
{
    public interface IAudioTranscriptionService
    {
        Task<string> GetAudioTranscriptFromCognitiveServicesAsync(Stream audioBlobStream);
    }

    public class AudioTranscriptionService : IAudioTranscriptionService
    {
        private static readonly string CognitiveServicesSpeechApiEndpoint = Environment.GetEnvironmentVariable("CognitiveServicesSpeechApiEndpoint");
        private static readonly string CognitiveServicesSpeechApiKey = Environment.GetEnvironmentVariable("CognitiveServicesSpeechApiKey");
        
        public async Task<string> GetAudioTranscriptFromCognitiveServicesAsync(Stream audioBlobStream)
        {
            var request = CreateAudioTranscriptRequest(audioBlobStream);

            var response = await request.GetResponseAsync();
            using (var stream = response.GetResponseStream())
            {
                var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                var responseString = reader.ReadToEnd();

                return ProcessAudioTranscriptResponse(responseString);
            }
        }

        protected internal HttpWebRequest CreateAudioTranscriptRequest(Stream audioBlobStream)
        {
            var request = (HttpWebRequest)WebRequest.Create(CognitiveServicesSpeechApiEndpoint);
            request.SendChunked = true;
            request.Accept = @"application/json";
            request.Method = "POST";
            request.ProtocolVersion = HttpVersion.Version11;
            request.ContentType = @"audio/wav; codec=audio/pcm; samplerate=16000";
            request.Headers["Ocp-Apim-Subscription-Key"] = CognitiveServicesSpeechApiKey;
   
            if (audioBlobStream.CanSeek)
            {
                audioBlobStream.Position = 0;
            }
            
            // open a request stream and write 1024 byte chunks in the stream one at a time
            using (var requestStream = request.GetRequestStream())
            {
                // read 1024 raw bytes from the input audio file
                var buffer = new byte[checked((uint) Math.Min(1024, (int)audioBlobStream.Length))];
                int bytesRead;
                while ((bytesRead = audioBlobStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    requestStream.Write(buffer, 0, bytesRead);
                }

                requestStream.Flush();
            }

            return request;
        }

        protected internal string ProcessAudioTranscriptResponse(string responseString)
        {
            dynamic responseJson = JObject.Parse(responseString);
            if (responseJson.RecognitionStatus != "Success")
            {
                return string.Empty;
            }

            var matches = responseJson.NBest;
            var bestMatch = matches.First;
            return bestMatch.Display;
        }
    }
}
