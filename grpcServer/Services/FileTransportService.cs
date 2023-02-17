using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using  grpcServer.Services;
using grpcFileTransportServer;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Google.Protobuf;
using System.Net.Sockets;
using System.Net;

namespace grpcServer.Services
{
    public class FileTransportService : FileService.FileServiceBase
    {
        readonly IWebHostEnvironment _webHostEnviroment;
        public  FileTransportService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnviroment = webHostEnvironment;

        }

        static async Task Main(string[] args)
        {
            var listener = new TcpListener(IPAddress.Any, 5220);
            listener.Start();

            // Wait for incoming connections
            while (true)
            {
               var client = await listener.AcceptTcpClientAsync();

               var stream = client.GetStream();
               var buffer = new byte[1024];

               // Read data from the client
               int bytesReceived = await stream.ReadAsync(buffer, 0, buffer.Length);

               if (bytesReceived > 0)
               {
                   // Process the received data
                   // ...

                  // var e = StringMessage.Parser.ParseFrom(buffer);

                  // Console.WriteLine($"{{DeviceID:{e.Integer}, EventID:{e.Message}}}");
               }
            }
        }

        public override async Task<Empty> FileUpload(IAsyncStreamReader<BytesContent> requestStream, ServerCallContext context)
        {
            //Streamin yapılacağı dizini belirliyoruz.
            string path = Path.Combine(_webHostEnviroment.WebRootPath, "files");
            if(!Directory.Exists(path)) Directory.CreateDirectory(path);
            
            FileStream? fileStream = null;
            try
            {
                int count  = 0;
                decimal chunkSize = 0;
                while (await requestStream.MoveNext())
                {
                    if (count++ == 0)
                    {
                        fileStream = new FileStream($"{path}/{requestStream.Current.Info.FileName}{requestStream.Current.Info.FileExtension}", FileMode.CreateNew);
                        fileStream.SetLength(requestStream.Current.FileSize);
                    }
                    var buffer = requestStream.Current.Buffer.ToByteArray();
                    if (fileStream != null) await fileStream.WriteAsync(buffer, 0, buffer.Length);

                    System.Console.WriteLine($"{Math.Round((chunkSize += requestStream.Current.ReadedByte * 100) / requestStream.Current.FileSize)}%");
                }
            }
            catch (System.Exception)
            {
                
                //Doldurucaz
            }

            if (fileStream != null)
            {
                await fileStream.DisposeAsync();
                fileStream.Close();
            } 
            
            return new Empty();
        }

        public override async Task FileDownload(grpcFileTransportServer.FileInfo request, IServerStreamWriter<BytesContent> responseStream, ServerCallContext context)
        {
            string path = Path.Combine(_webHostEnviroment.WebRootPath, "files");

            using FileStream fileStream = new FileStream($"{path}/{request.FileName}{request.FileExtension}", FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[2048];

            BytesContent content = new BytesContent
            {
                FileSize = fileStream.Length,
                Info = new grpcFileTransportServer.FileInfo{ FileName = Path.GetFileNameWithoutExtension(fileStream.Name), FileExtension = Path.GetExtension(fileStream.Name)},
                ReadedByte = 0
            }; 

            while ((content.ReadedByte = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                content.Buffer = ByteString.CopyFrom(buffer);
                await responseStream.WriteAsync(content);
            }
            fileStream.Close();
        }
    }
}