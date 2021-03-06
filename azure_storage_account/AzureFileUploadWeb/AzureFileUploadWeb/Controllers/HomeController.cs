﻿using AzureFileUploadWeb.Models;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FileUploadViewer.Controllers
{
    public class HomeController : Controller
    {
        string containerName = "";
        string StorageConnectionString = string.Empty;
        CloudBlobContainer storageContainer;

        public HomeController()
        {
            // 저장소 연결 문자열 가져오기
            StorageConnectionString = CloudConfigurationManager.GetSetting("StorageConnectionString");

            containerName = CloudConfigurationManager.GetSetting("ContainerName");

            // 저장소 계정 가져오기
            // 여기서 runtime 에러나면 Web.config에 가서 저장소 계정 액세스 키를 설정하세요
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);

            // blob 클라이언트 생성.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // 기존의 컨테이너 참조 가져오기
            storageContainer = blobClient.GetContainerReference(containerName);
        }

        public ActionResult Index()
        {
            List<BlobItem> blobList = this.LoadBlobLists();

            return View(blobList);
        }

        public ActionResult IndexJS()
        {
            List<BlobItem> blobList = this.LoadBlobLists();

            return View("IndexJS", blobList);
        }

        [HttpGet]
        public JsonResult GetBlobList()
        {
            return Json(this.LoadBlobLists(), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadFiles(IEnumerable<HttpPostedFileBase> files)
        {
            this.UploadFilesToAzureStorage(files);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// HTML 폼에서 업로드 된 파일드를 Azure Stoage로 업로드 하는 메서드.
        /// Javascript에서 직접 호출가능 및 내부에서 호출가능
        /// </summary>
        /// <param name="files">업로드된 파일들</param>
        private void UploadFilesToAzureStorage(IEnumerable<HttpPostedFileBase> files)
        {
            foreach (var file in files)
            {
                if (file?.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(file.FileName);

                    // Azure Storage로 파일 업로드 수행
                    CloudBlockBlob blockBlob = storageContainer.GetBlockBlobReference(fileName);
                    blockBlob.UploadFromStream(file.InputStream);
                }
            }
        }

        /// <summary>
        /// FireFox나 Chrome에서는 IEnumerable<HttpPostedFileBase>로 받을 경우 올바로 인식되지 않음
        /// 2개의 파일이 올라오면 올바로 인식되나 1개만 업로드 된 경우에는 null로 인식됨.
        /// 해서 가장 기본적인 유형의 메서드를 따로 정의..  최적화는 여러분의 손에!
        /// </summary>
        [HttpPost]
        public void UploadFilesFromChrome(IEnumerable<HttpPostedFileBase> files)
        {
            if (Request.Files.Count > 0)
            {
                for (int num = 0; num < Request.Files.Count; num++)
                {
                    string fileName = Path.GetFileName(Request.Files[num].FileName);
                    if (Request.Files[num] != null && Request.Files[num].ContentLength > 0)
                    {
                        // Azure Storage로 파일 업로드 수행
                        CloudBlockBlob blockBlob = storageContainer.GetBlockBlobReference(fileName);
                        blockBlob.UploadFromStream(Request.Files[num].InputStream);
                    }
                }
            }
        }

        private List<BlobItem> LoadBlobLists()
        {
            List<BlobItem> blobList = new List<BlobItem>();

            // BLOB 목록을 가져와서 각각에 대한 정보로 List를 채움. 해당 List는 그리드로 바인딩함
            foreach (IListBlobItem item in storageContainer.ListBlobs(null, false))
            {
                if (item.GetType() == typeof(CloudBlockBlob) || item.GetType() == typeof(CloudPageBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;

                    string blobType = blob.Properties.BlobType.ToString();
                    long blobSize =  blob.Properties.Length;
                    Uri blobUri = blob.Uri;
                    string blobName = blob.Name;

                    blobList.Add(new BlobItem()
                        { Name = blobName, Type = blobType, Size = blobSize, URL = blobUri.ToString() }
                    );

                }
                else if (item.GetType() == typeof(CloudBlobDirectory))
                {
                    // 디렉토리의 경우 처리할 사항들....
                }
            }

            return blobList;
        }
    }
}