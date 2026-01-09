using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Grpc.Auth;
using Grpc.Core;
using System;
using System.Configuration;
using System.IO;

namespace Fitpad.Services
{
    /// <summary>
    /// Singleton provider for FirestoreDb instance.
    /// Manages credentials and configuration in one place.
    /// </summary>
    public sealed class FirestoreDbProvider
    {
        private static readonly Lazy<FirestoreDbProvider> _instance = 
            new Lazy<FirestoreDbProvider>(() => new FirestoreDbProvider());

        private readonly FirestoreDb _firestoreDb;

        public static FirestoreDbProvider Instance => _instance.Value;

        private FirestoreDbProvider()
        {
            // Read configuration
            var projectId = ConfigurationManager.AppSettings["ProjectId"];
            var keyFileName = ConfigurationManager.AppSettings["GoogleCredentialsFileName"];

            if (string.IsNullOrWhiteSpace(projectId))
                throw new InvalidOperationException("ProjectId is not configured in App.config");

            if (string.IsNullOrWhiteSpace(keyFileName))
                throw new InvalidOperationException("GoogleCredentialsFileName is not configured in secrets.config");

            // Build path to credentials
            string pathToKeyFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", keyFileName);

            if (!File.Exists(pathToKeyFile))
                throw new FileNotFoundException($"Файл учетных данных не найден по пути: {pathToKeyFile}");

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
        }

        /// <summary>
        /// Get the shared FirestoreDb instance.
        /// </summary>
        public FirestoreDb GetDb() => _firestoreDb;
    }
}
