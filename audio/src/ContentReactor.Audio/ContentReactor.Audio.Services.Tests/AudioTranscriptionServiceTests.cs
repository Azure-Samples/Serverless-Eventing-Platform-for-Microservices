using System;
using System.IO;
using Xunit;

namespace ContentReactor.Audio.Services.Tests
{
    public class AudioTranscriptionServiceTests
    {
        [Fact]
        public void CreateAudioTranscriptRequest_ReturnsExpectedRequest()
        {
            // arrange
            Environment.SetEnvironmentVariable("CognitiveServicesSpeechApiEndpoint", "http://fakeendpoint");
            Environment.SetEnvironmentVariable("CognitiveServicesSpeechApiKey", "fakekey");
            var stream = new MemoryStream();
            var service = new AudioTranscriptionService();

            // act
            var result = service.CreateAudioTranscriptRequest(stream);

            // assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ProcessAudioTranscriptResponse_ReturnsExpectedResponse()
        {
            // arrange
            const string responseString = "{\"RecognitionStatus\":\"Success\",\"Offset\":3600000,\"Duration\":89800000,\"NBest\":[{\"Confidence\":0.940092,\"Lexical\":\"hi i\'m brian one of the available high-quality text to speech voices select download not to install my voice\",\"ITN\":\"hi I\'m Brian one of the available high-quality text to speech voices select download not to install my voice\",\"MaskedITN\":\"hi I\'m Brian one of the available high-quality text to speech voices select download not to install my voice\",\"Display\":\"Hi I\'m Brian one of the available high-quality text to speech voices select download not to install my voice.\"},{\"Confidence\":0.929836333,\"Lexical\":\"hi i\'m brian one of the available high-quality text to speech voices select download now to install my voice\",\"ITN\":\"hi I\'m Brian one of the available high-quality text to speech voices select download now to install my voice\",\"MaskedITN\":\"hi I\'m Brian one of the available high-quality text to speech voices select download now to install my voice\",\"Display\":\"Hi I\'m Brian one of the available high-quality text to speech voices select download now to install my voice.\"},{\"Confidence\":0.9099141,\"Lexical\":\"hi i\'m bryan one of the available high-quality text to speech voices select download not to install my voice\",\"ITN\":\"hi I\'m Bryan one of the available high-quality text to speech voices select download not to install my voice\",\"MaskedITN\":\"hi I\'m Bryan one of the available high-quality text to speech voices select download not to install my voice\",\"Display\":\"Hi I\'m Bryan one of the available high-quality text to speech voices select download not to install my voice.\"},{\"Confidence\":0.9099141,\"Lexical\":\"hi i\'m brian one of the available high-quality text to speech voices select download not too install my voice\",\"ITN\":\"hi I\'m Brian one of the available high-quality text to speech voices select download not too install my voice\",\"MaskedITN\":\"hi I\'m Brian one of the available high-quality text to speech voices select download not too install my voice\",\"Display\":\"Hi I\'m Brian one of the available high-quality text to speech voices select download not too install my voice.\"},{\"Confidence\":0.8996583,\"Lexical\":\"hi i\'m bryan one of the available high-quality text to speech voices select download now to install my voice\",\"ITN\":\"hi I\'m Bryan one of the available high-quality text to speech voices select download now to install my voice\",\"MaskedITN\":\"hi I\'m Bryan one of the available high-quality text to speech voices select download now to install my voice\",\"Display\":\"Hi I\'m Bryan one of the available high-quality text to speech voices select download now to install my voice.\"}]}";
            var service = new AudioTranscriptionService();

            // act
            var response = service.ProcessAudioTranscriptResponse(responseString);

            // assert
            Assert.Equal("Hi I'm Brian one of the available high-quality text to speech voices select download not to install my voice.", response);
        }
    }
}
