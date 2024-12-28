using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Admin.Services
{
    public class FirebaseService
    {
        private FirestoreDb _firestoreDb;
        private StorageClient _storageClient;
        private readonly string _bucketName = "smartgear-d4463.firebasestorage.app";

        // Constructor to initialize Firestore and Storage client
        public FirebaseService()
        {
            // Đảm bảo FirebaseApp đã được tạo ở nơi khác, không cần tạo lại ở đây
            _firestoreDb = FirestoreDb.Create("smartgear-d4463");
            _storageClient = StorageClient.Create();
        }

        // Get collection from Firestore with added 'id' field
        public async Task<List<Dictionary<string, object>>> GetCollectionAsync(string collectionName)
        {
            var collection = _firestoreDb.Collection(collectionName);
            var snapshot = await collection.GetSnapshotAsync();
            var result = new List<Dictionary<string, object>>();

            foreach (var document in snapshot.Documents)
            {
                var docDict = document.ToDictionary();
                docDict["id"] = document.Id; // Add the document id to the dictionary
                result.Add(docDict);
            }
            return result;
        }

        // Upload an image to Firebase Storage
        public async Task<string> UploadImageAsync(IFormFile image)
        {
            try
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

                using (var stream = image.OpenReadStream())
                {
                    // Upload image
                    var objectName = "images/" + fileName;
                    var obj = await _storageClient.UploadObjectAsync(_bucketName, objectName, image.ContentType, stream);

                    // Tạo URL với query parameter 'alt=media'
                    var url = $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{Uri.EscapeDataString(objectName)}?alt=media";
                    return url;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading image: {ex.Message}");
                return null; // Hoặc throw exception nếu cần
            }
        }

        // Add a new document to Firestore
        public async Task AddDocumentAsync(string collectionName, Dictionary<string, object> data)
        {
            var collection = _firestoreDb.Collection(collectionName);
            await collection.AddAsync(data);
        }

        // Update an existing document in Firestore
        public async Task UpdateDocumentAsync(string collectionName, string documentId, Dictionary<string, object> data)
        {
            var document = _firestoreDb.Collection(collectionName).Document(documentId);
            await document.SetAsync(data, SetOptions.MergeAll);
        }

        // Delete a document from Firestore
        public async Task DeleteDocumentAsync(string collectionName, string documentId)
        {
            var document = _firestoreDb.Collection(collectionName).Document(documentId);
            await document.DeleteAsync();
        }

        public async Task DeleteImageAsync(string imageUrl)
        {
            try
            {
                // Extract file name from URL
                var fileName = imageUrl.Split('/').Last();
                await _storageClient.DeleteObjectAsync(_bucketName, "images/" + fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete image: {ex.Message}");
            }
        }

        public async Task DeleteProductAndImagesAsync(string collectionName, string documentId)
        {
            try
            {
                var documentRef = _firestoreDb.Collection(collectionName).Document(documentId);
                var snapshot = await documentRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    Console.WriteLine($"Document {documentId} does not exist.");
                    return;
                }

                var data = snapshot.ToDictionary();

                // Delete associated images
                if (data.ContainsKey("p_imgs") && data["p_imgs"] is List<object> imageUrls)
                {
                    foreach (var imageUrl in imageUrls.Cast<string>())
                    {
                        try
                        {
                            await DeleteImageAsync(imageUrl);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete image {imageUrl}: {ex.Message}");
                        }
                    }
                }

                // Delete document from Firestore
                await documentRef.DeleteAsync();
                Console.WriteLine($"Document {documentId} has been deleted with its images");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete product and images: {ex.Message}");
            }
        }
    }
}
