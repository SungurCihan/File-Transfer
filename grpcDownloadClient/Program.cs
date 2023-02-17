using Grpc.Net.Client;
using System;
using grpcFileTransportDownloadClient;

namespace grpcDownloadClient 
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var channel = GrpcChannel.ForAddress("http://localhost:5220");
            var client = new FileService.FileServiceClient(channel);

            string pathToDownload = @"C:\gRPC File Transfer\grpcDownloadClient\DownloadedFiles";

            var fileInfo = new grpcFileTransportDownloadClient.FileInfo
            {
                FileExtension = ".CR2",
                FileName = "BF4A0012"
            };

            FileStream? fileStream = null;

            var request = client.FileDownload(fileInfo);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            int count = 0;
            decimal chunkSize = 0;
            while (await request.ResponseStream.MoveNext(cancellationTokenSource.Token))
            {
                if (count++ == 0)
                {
                    fileStream = new FileStream(@$"{pathToDownload}\{request.ResponseStream.Current.Info.FileName}{request.ResponseStream.Current.Info.FileExtension}", FileMode.CreateNew);
                    fileStream.SetLength(request.ResponseStream.Current.FileSize);
                }

                var buffer = request.ResponseStream.Current.Buffer.ToByteArray();
                if (fileStream != null) await fileStream.WriteAsync(buffer, 0, request.ResponseStream.Current.ReadedByte);
                 System.Console.WriteLine($"{Math.Round((chunkSize += request.ResponseStream.Current.ReadedByte * 100) / request.ResponseStream.Current.FileSize)}%");
            }

            System.Console.WriteLine("Yüklendi...");
            if(fileStream != null)
            {
                await fileStream.DisposeAsync();
                fileStream.Close();
            }
        }
    }
}