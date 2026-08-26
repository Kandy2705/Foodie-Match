using System.Collections.Generic;
using Firebase.Firestore;

namespace FoodieMatch.Infrastructure.Firebase.PlayerProfiles
{
    internal sealed class FirestorePlayerProfileDocument
    {
        private const string SchemaVersionField = "schemaVersion";
        private const string RevisionField = "revision";
        private const string ProfileJsonField = "profileJson";
        private const string CreatedAtField = "createdAt";
        private const string UpdatedAtField = "updatedAt";

        private FirestorePlayerProfileDocument(
            int schemaVersion,
            long revision,
            string profileJson)
        {
            SchemaVersion = schemaVersion;
            Revision = revision;
            ProfileJson = profileJson;
        }

        public int SchemaVersion { get; }

        public long Revision { get; }

        public string ProfileJson { get; }

        public static bool TryRead(
            DocumentSnapshot snapshot,
            out FirestorePlayerProfileDocument document,
            out string errorMessage)
        {
            if (!snapshot.TryGetValue(
                    SchemaVersionField,
                    out long schemaVersion) ||
                schemaVersion < int.MinValue ||
                schemaVersion > int.MaxValue)
            {
                document = null;
                errorMessage =
                    "Cloud player profile schema version is missing or invalid.";
                return false;
            }

            if (!snapshot.TryGetValue(RevisionField, out long revision) ||
                revision < 0)
            {
                document = null;
                errorMessage =
                    "Cloud player profile revision is missing or invalid.";
                return false;
            }

            if (!snapshot.TryGetValue(ProfileJsonField, out string profileJson) ||
                string.IsNullOrWhiteSpace(profileJson))
            {
                document = null;
                errorMessage = "Cloud player profile JSON is missing.";
                return false;
            }

            document = new FirestorePlayerProfileDocument(
                (int)schemaVersion,
                revision,
                profileJson);
            errorMessage = null;
            return true;
        }

        public static bool TryReadRevision(
            DocumentSnapshot snapshot,
            out long revision)
        {
            return snapshot.TryGetValue(RevisionField, out revision) &&
                   revision >= 0;
        }

        public static Dictionary<string, object> CreateWriteData(
            int schemaVersion,
            long revision,
            string profileJson,
            bool includeCreatedAt)
        {
            Dictionary<string, object> data = new()
            {
                [SchemaVersionField] = (long)schemaVersion,
                [RevisionField] = revision,
                [ProfileJsonField] = profileJson,
                [UpdatedAtField] = FieldValue.ServerTimestamp
            };

            if (includeCreatedAt)
            {
                data[CreatedAtField] = FieldValue.ServerTimestamp;
            }

            return data;
        }
    }
}
