﻿//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------


using Microsoft.Identity.Client;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Configuration;

namespace CT.AccessToken.Client
{
    class Program
    {
        private const char Separator = ',';
        #region Static Fields

        private static PublicClientApplication application;
        private static RestClient restClient;
        private static AccessTokenClient client = new AccessTokenClient();
        private static string clientId;
        private static string workspaceId;
        private static string authorityUri;
        private static string apiEndpoint;
        private static List<string> scopes;

        #endregion
        static void Main(string[] args)
        {
            client.LoadCredentials();
            clientId = AccessTokenClient.ClientId;
            workspaceId = AccessTokenClient.WorkspaceId;
            authorityUri = AccessTokenClient.EndPointOauthV2;
            apiEndpoint = AccessTokenClient.EndPointAddressV1Prod;
            scopes = AccessTokenClient.Scopes;

            //application = new PublicClientApplication(clientId, authorityUri, CachePersistence.GetUserCache());
            application = new PublicClientApplication(clientId, authorityUri, null);
            string idToken = null;
            try
            {
                idToken = AcquireTokenSilent();
            }
            catch
            {
                idToken = AcquireTokenWithSignIn();
            }

            Console.WriteLine("Id token Acquired Successfully. Use http://jwt.ms/ to inspect the token.");
            Console.WriteLine("Token:");
            Console.WriteLine(idToken);
            Console.WriteLine();

            // Get the categories from the Custom Translator api. This just tests that we have a valid auth token.
            Console.WriteLine("Calling Custom Translator categories API to verify auth...");
            restClient = new RestClient(apiEndpoint);

            Console.WriteLine();
            Console.WriteLine("Get workspace list");
            GetWorkspaces(idToken);

            Console.WriteLine();
            Console.WriteLine("Get project list");
            GetCategories(idToken);

            Console.WriteLine();
            Console.WriteLine("Get document list");
            GetDocuments(idToken);

            Console.WriteLine();
            Console.WriteLine("Post Create Model and Train");
            string modelName = string.Concat(ConfigurationManager.AppSettings["modelNamePrefix"], DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            string pjId = ConfigurationManager.AppSettings["projectId"];
            List<string> docIdsStr = ConfigurationManager.AppSettings["documentIds"].Split(Separator).ToList<string>();
            List<int> docIds = new List<int>();
            foreach(string docIdStr in docIdsStr){
                docIds.Add(int.Parse(docIdStr));
            }
            
            var modelParam = new ModelCreateRequest()
            {
                name = modelName,
                projectId = pjId,
                documentIds = docIds,
                isAutoDeploy = true,
                isTestingAuto = true,
                isTuningAuto = true,
            };

            PostCreateModelRequestItem(idToken, modelParam);

            Console.WriteLine();
            Console.WriteLine($"Press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// The silent sign-in. Relies on token cache.
        /// </summary>
        /// <returns></returns>
        public static string AcquireTokenSilent()
        {
            var accounts = application.GetAccountsAsync().Result;
            var result = application.AcquireTokenSilentAsync(scopes, accounts.FirstOrDefault()).Result;
            return result.IdToken;
        }

        /// <summary>
        /// The INTERACTIVE sign in action. It redirects to AAD to sign the user in and get back the token of the user. 
        /// </summary>
        /// <returns></returns>        
        public static string AcquireTokenWithSignIn()
        {
            AuthenticationResult result = application.AcquireTokenAsync(scopes).Result;
            return result.IdToken;
        }

        public static void GetCategories(string token)
        {
            //RestRequest request = new RestRequest($"/api/texttranslator/v1.0/categories", Method.GET);
            //request.AddHeader("Authorization", "Bearer " + token);

            IRestResponse response = client.GetItem(token, $"projects?workspaceId={workspaceId}&pageIndex=1");

            Console.WriteLine($"StatusCode: {response.StatusCode}");
            Console.WriteLine($"Description: {response.StatusDescription}");
            Console.WriteLine($"Headers: {response.Headers}");
            Console.WriteLine($"Content: {response.Content}");
        }

        private static void GetWorkspaces(string token)
        {
            IRestResponse response = client.GetItem(token, $"workspaces");

            Console.WriteLine($"StatusCode: {response.StatusCode}");
            Console.WriteLine($"Description: {response.StatusDescription}");
            Console.WriteLine($"Headers: {response.Headers}");
            Console.WriteLine($"Content: {response.Content}");
        }

        private static void GetDocuments(string token)
        {
            IRestResponse response = client.GetItem(token, $"documents?workspaceId={workspaceId}&pageIndex=1");

            Console.WriteLine($"StatusCode: {response.StatusCode}");
            Console.WriteLine($"Description: {response.StatusDescription}");
            Console.WriteLine($"Headers: {response.Headers}");
            Console.WriteLine($"Content: {response.Content}");
        }

        private static void PostCreateModelRequestItem(string token, ModelCreateRequest param)
        {
            IRestResponse response = client.CreateModelRequest(token, param);

            Console.WriteLine($"StatusCode: {response.StatusCode}");
            Console.WriteLine($"Description: {response.StatusDescription}");
            Console.WriteLine($"Headers: {response.Headers}");
            Console.WriteLine($"Content: {response.Content}");

        }
    }
}
