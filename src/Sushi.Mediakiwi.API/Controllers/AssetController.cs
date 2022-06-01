﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sushi.Mediakiwi.API.Filters;
using Sushi.Mediakiwi.API.Transport;
using Sushi.Mediakiwi.API.Transport.Requests;
using Sushi.Mediakiwi.API.Transport.Responses;
using Sushi.Mediakiwi.Data.Configuration;
using Sushi.Mediakiwi.Logic;
using Sushi.Mediakiwi.Persisters;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sushi.Mediakiwi.API.Controllers
{
    [MediakiwiApiAuthorize]
    [Route(Common.MK_CONTROLLERS_PREFIX + "asset")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AssetController : ControllerBase
    {
        private readonly AssetService _assetService;

        public AssetController(AssetService assetService)
        {
            _assetService = assetService;
        }
        
        private static string Azure_Image_Container
        {
            get
            {
                return WimServerConfiguration.Instance?.Azure_Image_Container;
            }
        }

        private static string Azure_Cdn_Uri
        {
            get
            {
                return WimServerConfiguration.Instance?.Azure_Cdn_Uri;
            }
        }

        private static BlobPersister GetPersistor
        {
            get { return new BlobPersister(); }
        }

        protected Data.IApplicationUser MediakiwiUser
        {
            get
            {
                if (HttpContext.Items.ContainsKey(Common.API_USER_CONTEXT))
                {
                    return HttpContext.Items[Common.API_USER_CONTEXT] as Data.IApplicationUser;
                }
                else
                {
                    return null;
                }
            }
        }

        private System.Drawing.Image CreateThumbnailImage(System.Drawing.Image input)
        {
            try
            {
                var imageHeight = input.Height;
                var imageWidth = input.Width;

                var maxThumbWidth = System.Math.Max(64, WimServerConfiguration.Instance.Thumbnails.CreateThumbnailWidth);
                var maxThumbHeight = System.Math.Max(48, WimServerConfiguration.Instance.Thumbnails.CreateThumbnailHeight);

                if (imageHeight > imageWidth)
                {
                    var factor = ((float)maxThumbHeight / (float)imageHeight);
                    imageWidth = (int)(factor * imageWidth);
                    imageHeight = maxThumbHeight;

                    // Resulting thumbnail is wider then the supplied max thumb width,
                    // recalculate height
                    if (imageWidth > maxThumbWidth)
                    {
                        factor = ((float)maxThumbWidth / (float)input.Width);
                        imageHeight = (int)(factor * input.Height);
                        imageWidth = maxThumbWidth;
                    }
                }
                else
                {
                    var factor = ((float)maxThumbWidth / (float)imageWidth);
                    imageHeight = (int)(factor * imageHeight);
                    imageWidth = maxThumbWidth;

                    // Resulting thumbnail is higher then the supplied max thumb height,
                    // recalculate width
                    if (imageHeight > maxThumbHeight) 
                    {
                        factor = ((float)maxThumbHeight / (float)input.Height);
                        imageWidth = (int)(factor * input.Width);
                        imageHeight = maxThumbHeight;
                    }
                }

                return input.GetThumbnailImage(imageWidth, imageHeight, () => false, System.IntPtr.Zero);

            }
            catch (System.Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns all galleries that are available for the supplied user
        /// </summary>
        /// <returns></returns>
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet("GetGalleries")]
        public async Task<ActionResult<List<ListItemCollectionOption>>> GetGalleries()
        {
            var temp = new List<ListItemCollectionOption>();

            var galleries = await Data.Gallery.SelectAllAccessibleAsync(MediakiwiUser);
            foreach (var gallery in galleries)
            {
                temp.Add(new ListItemCollectionOption() 
                { 
                    Text = gallery.CompletePath, 
                    Value = gallery.ID.ToString(), 
                    IsEnabled = true 
                });
            }

            return Ok(temp);
        }

        /// <summary>
        /// Saves the supplied asset to Azure and returns information about the created/updated asset
        /// </summary>
        /// <param name="request">Contains all information about the asset</param>
        /// <returns></returns>
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost("SaveAsset")]
        public async Task<ActionResult<SaveAssetResponse>> SaveAsset([FromForm] SaveAssetRequest request)
        {
            // Request is empty
            if (request == null)
            {
                return BadRequest();
            }

            // We received a filename without extension, throw an error
            if (request.Data?.FileName?.Contains(".", System.StringComparison.InvariantCulture) == false)
            {
                return BadRequest();
            }

            // New image
            if (request.ID.GetValueOrDefault(0) == 0)
            {
                // But no data supplied
                if (request.Data?.Length == 0 || string.IsNullOrWhiteSpace(request.Data?.FileName) == true)
                {
                    return BadRequest();
                }

                // But no gallery supplied
                if (request.GalleryID.GetValueOrDefault(0) == 0)
                {
                    return BadRequest();
                }
            }

            // The target asset
            Data.Asset asset = new Data.Asset();

            // We're updating an asset, get it from DB
            if (request.ID.GetValueOrDefault(0) > 0)
            {
                asset = await Data.Asset.SelectOneAsync(request.ID.Value);
            }

            // Update gallery when set
            if (request.GalleryID.GetValueOrDefault(0) > 0)
            {
                asset.GalleryID = request.GalleryID.Value;
            }

            asset.Description = request.Description;
            if (string.IsNullOrWhiteSpace(request.Title) == false)
            {
                asset.Title = request.Title;
            }

            if (string.IsNullOrWhiteSpace(asset.Title))
            {
                asset.Title = asset.FileName;
            }

            // We have new data provided, process it
            if (request.Data != null)
            {
                asset.Type = request.Data.ContentType;
                asset.Extension = request.Data.FileName.Substring(request.Data.FileName.LastIndexOf('.'));

                // upload to blob and save
                using var stream = request.Data.OpenReadStream();
                await _assetService.UpsertAssetAsync(asset, stream, Azure_Image_Container, request.Data.FileName, request.Data.ContentType);
            }
            else
            {
                // only update metadata
                await asset.SaveAsync();
            }

            return Ok(new SaveAssetResponse() 
            {
                Description = asset.Description,
                Title = asset.Title,
                GalleryID = asset.GalleryID,
                ID = asset.ID,
                RemoteLocation = asset.RemoteLocation,
                RemoteLocationThumb  = asset.RemoteLocation_Thumb
            });
        }
    }
}
