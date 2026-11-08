using Application.Dtos.Default;
using Domain.Entitites;
using Infrastructure.Repositories.BaseRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.AppFileService
{
    public class AppFileService : IAppFileService
    {
        private readonly IBaseRepository<AppFile> _appFileRepository;
        private readonly IBaseRepository<StoredFile> _storedFileRepository;

        public AppFileService(
             IBaseRepository<AppFile> appFileRepository,
             IBaseRepository<StoredFile> storedFileRepository
        ) {
            _appFileRepository = appFileRepository;
            _storedFileRepository = storedFileRepository;
        }
        
        public BaseResponse<AppFile> InsertFile(IFormFile req)
        {
            var file = new AppFile();
            var storedFile = new StoredFile();
            var stream = req.OpenReadStream();
            
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo( memoryStream );

                storedFile.Create(
                    req.FileName,
                    req.ContentType,
                     Convert.ToBase64String(memoryStream.ToArray()) 
                );
            }
                
            var storedFileAddedFile = _storedFileRepository.AddAsync(storedFile).Result;

            file.Create(
                storedFileAddedFile
            );

            var appFileAddResult = _appFileRepository.AddAsync(file).Result;
            var response = new BaseResponse<AppFile>(appFileAddResult is not null && storedFileAddedFile is not null);

            if (!response.Success)
            {
                response.AddError("Não foi possivel completar a operação");
            }
            else
            {
                response.Data = file;
            }

            return response;
        }
        private const int MaxIconBytes = 2 * 1024 * 1024;
        private static readonly HttpClient IconClient = new() { Timeout = TimeSpan.FromSeconds(8) };

        public BaseResponse<AppFile> InsertFileUrl(string imageUrl)
        {
            var file = new AppFile();
            var storedFile = new StoredFile();

            var failed = new BaseResponse<AppFile>(false);
            failed.AddError("Não foi possível baixar a imagem da URL");

            // The URL comes from a captured page: only web images, small, and never waiting long
            // (without internet, e.g. behind a VPN, the request would hang for minutes).
            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return failed;
            }

            try
            {
                using var response = IconClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).Result;
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
                var length = response.Content.Headers.ContentLength;

                if (!response.IsSuccessStatusCode || !contentType.StartsWith("image/") || length > MaxIconBytes)
                {
                    return failed;
                }

                var bytes = response.Content.ReadAsByteArrayAsync().Result;
                if (bytes.Length > MaxIconBytes)
                {
                    return failed;
                }

                storedFile.Create(
                    Path.GetFileName(uri.LocalPath),
                    contentType,
                    Convert.ToBase64String(bytes)
                );
            }
            catch (Exception)
            {
                // Timeout, DNS or connection failure.
                return failed;
            }

            var storedFileAddedFile = _storedFileRepository.AddAsync(storedFile).Result;

            file.Create(storedFileAddedFile);

            var appFileAddResult = _appFileRepository.AddAsync(file).Result;
            var result = new BaseResponse<AppFile>(appFileAddResult is not null && storedFileAddedFile is not null);

            if (!result.Success)
            {
                result.AddError("Não foi possivel completar a operação");
            }
            else
            {
                result.Data = file;
            }

            return result;
        }
        public DefaultResponse DeleteFile(int id)
        {
            var appFile = _appFileRepository.Get().FirstOrDefault(e => e.Id == id);
            var storedFile = _storedFileRepository.Get().FirstOrDefault(e => e.Id == appFile.StoredFileId);
            var removeAppFileResult = _appFileRepository.Remove(appFile);
            var removeStoredFileResult = _storedFileRepository.Remove(storedFile);
            var response = new DefaultResponse(removeAppFileResult && removeStoredFileResult);

            if (!response.Success)
            {
                response.AddError("Não foi possivel completar a operação");
            }

            return response;
        }
        public BaseResponse<List<AppFile>> GetFiles()
        {
            var result = _appFileRepository.Get()
                .OrderByDescending(e=>e.Id)
                .Include(e=>e.StoredFile)
                .ToList();
            var response = new BaseResponse<List<AppFile>>(result is not null);

            if (!response.Success)
            {
                response.AddError("Não foi possivel completar a operação");
            }
            else
            {
                response.Data = result;
            }

            return response;
        }
    }
}
