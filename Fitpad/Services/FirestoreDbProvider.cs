using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Grpc.Auth;
using Grpc.Core;
using System;
using System.Configuration;
using System.IO;
using NLog;

namespace Fitpad.Services
{
    /// <summary>
    /// Singleton provider for FirestoreDb instance.
    /// Manages credentials and configuration in one place.
    /// </summary>
    public sealed class FirestoreDbProvider
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Lazy<FirestoreDbProvider> _instance = 
            new Lazy<FirestoreDbProvider>(() => new FirestoreDbProvider());

        private readonly FirestoreDb _firestoreDb;

        public static FirestoreDbProvider Instance => _instance.Value;

        private FirestoreDbProvider()
        {
            // Read configuration
            var projectId = ConfigurationManager.AppSettings["ProjectId"];
            var keyFileName = ConfigurationManager.AppSettings["GoogleCredentialsFileName"];

            Logger.Debug($"Инициализация FirestoreDb для проекта: {projectId}");

            if (string.IsNullOrWhiteSpace(projectId))
            {
                Logger.Fatal("ProjectId не настроен в App.config");
                throw new InvalidOperationException("ProjectId is not configured in App.config");
            }

            if (string.IsNullOrWhiteSpace(keyFileName))
            {
                Logger.Fatal("GoogleCredentialsFileName не настроен в secrets.config");
                throw new InvalidOperationException("GoogleCredentialsFileName is not configured in secrets.config");
            }

            // Build path to credentials
            string pathToKeyFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", keyFileName);

            if (!File.Exists(pathToKeyFile))
            {
                Logger.Fatal($"Credential файл не найден: {pathToKeyFile}");
                throw new FileNotFoundException($"Файл учетных данных не найден по пути: {pathToKeyFile}");
            }

            Logger.Debug($"Загружен credential файл: {keyFileName}");

            // Set environment variable for Google libraries
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToKeyFile);

            // Create credentials
            GoogleCredential credential = GoogleCredential.FromFile(pathToKeyFile);
            ChannelCredentials channelCredentials = credential.ToChannelCredentials();

            // Build FirestoreDb
            _firestoreDb = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                ChannelCredentials = channelCredentials
            }.Build();

            Logger.Info($"FirestoreDb успешно инициализирован для проекта: {projectId}");
        }

        /// <summary>
        /// Get the shared FirestoreDb instance.
        /// </summary>
        public FirestoreDb GetDb() => _firestoreDb;
    }
}
