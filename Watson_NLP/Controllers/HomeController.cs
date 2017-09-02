using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IBM.WatsonDeveloperCloud.SpeechToText.v1;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1;
using IBM.WatsonDeveloperCloud.Http;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1.Model;
using IBM.WatsonDeveloperCloud.TextToSpeech.v1;
using IBM.WatsonDeveloperCloud.TextToSpeech.v1.Model;
using System.IO;
using System.Diagnostics;
using Watson_NLP.Constants;

namespace Watson_NLP.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public JsonResult ProcesarWatsonSTT(){

            SpeechToTextService speech_to_text = new SpeechToTextService();
            speech_to_text.SetCredential(Auth.Watson_STT.Username, Auth.Watson_STT.Password);
            FileStream fs = System.IO.File.OpenRead("resources/audio.flac");
            var result = speech_to_text.Recognize("audio/flac",fs,"","es-ES_BroadbandModel");
            List<String> transcripts = new List<string>();
            foreach(var item in result.Results) {
                foreach(var alt in item.Alternatives) {
                    transcripts.Add(alt.Transcript);
                }
            }

            return Json(transcripts);
        }

        public JsonResult ProcesarWatsonNLU(String texto) {
            
            object result = null;
            try{
                NaturalLanguageUnderstandingService natural_language_und = new NaturalLanguageUnderstandingService(Auth.Watson_NLU.Username, Auth.Watson_NLU.Password, Auth.Watson_NLU.VersionDate);
                
                Parameters parameters = new Parameters() { 
                    Text = texto,
                    Features = new Features()
                    {
                        Keywords = new KeywordsOptions()
                        {
                            Limit = 8,
                            Sentiment = true,
                            Emotion = true
                        },

                        Entities = new EntitiesOptions(){
                            Limit = 8,
                            Sentiment = true,
                            Emotion = true
                        },

                        SemanticRoles = new SemanticRolesOptions() {
                            
                        }
                    }
                };

                var resultado = natural_language_und.Analyze(parameters);

                var rolesSemanticos = resultado.SemanticRoles;
                bool accionAbrir = false;
                bool word = false;

                foreach (var item in rolesSemanticos)
                {
                    if(item.Action != null){
                        if(item.Action.Text != null){
                            string accion = item.Action.Text.ToLower();
                            if(accion.Contains("abre") || accion.Contains("abrir") || accion.Contains("ejecutar") || accion.Contains("lanzar")) accionAbrir = true;
                        }
                    }

                    if(item.Subject != null){
                        if(item.Subject.Text != null){
                            string sujeto = item.Subject.Text.ToLower();
                            if(sujeto.Contains("word")) word = true;
                        }
                    }

                    if(item._Object != null){
                        if(item._Object.Text != null){
                            string objeto = item._Object.Text.ToLower();
                            if(objeto.Contains("word")) word = true;
                        }
                    }

                    if(word && accionAbrir) break;
                }

                
                if(word && accionAbrir) Process.Start(ExcecutablePaths.MicrosoftWord);

                result = new {
                    data = resultado,
                    status = "Ok"
                };

            }catch(Exception ex){
                result = new {
                    data = ex.Message,
                    status = "Error"
                };
            }

            return Json(result);
        }

        public JsonResult ProcesarWatsonTTS(String texto) {
            object result = null;

            try
            {
                TextToSpeechService text_to_speech = new TextToSpeechService(Auth.Watson_TTS.Username, Auth.Watson_TTS.Password);
                var results = text_to_speech.Synthesize(texto, Voice.ES_SOFIA, AudioType.FLAC);
                var fileStream = new FileStream("resources/tts.flac", FileMode.Create, FileAccess.Write);
                results.CopyTo(fileStream);
                fileStream.Dispose();

                result = new {
                    data = "tts.flac",
                    status = "Ok"
                };
            }
            catch (Exception ex)
            {
                result = new {
                    data = ex.Message,
                    status = "Error"
                };
            }

            return Json(result);
        }
        
        public IActionResult Error()
        {
            return View();
        }
    }
}
