using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Tradify.Core.Bases;
using Tradify.Core.Features.Instructor.Command.Models;
using Tradify.Core.Features.Seller.Command.Models;
using Tradify.Core.Features.Seller.Queries.Results;
using Tradify.Core.Features.User.Commands.Models;
using Tradify.Core.Resources.Service;
using Tradify.Data.Entities;
using Tradify.Data.Enums;
using Tradify.Data.Helpers;
using Tradify.Data.Helpers.Results;
using Tradify.Infrastructure.AbstractsRepositories;
using Tradify.Service.AbstractsServices;
using Tradify.Service.AbstractsServices.AuthorizationServices;
using Tradify.Service.AbstractsServices.IdentityServices;
using Tradify.Service.Services;
using Tradify.Service.Services.IdentityServices;

namespace Tradify.Core.Features.Seller.Command.Handlers
{
    public class SellerCommandHandler : ResponseHandler
                                                      , IRequestHandler<AddSellerCommand, Response<string>>
                                                      , IRequestHandler<UpdateSellerCommand, Response<string>>
                                                      , IRequestHandler<ActiveSellerCommand, Response<string>>
                                                      , IRequestHandler<DisActiveSellerCommand, Response<string>>
                                                      , IRequestHandler<GetUserByTokenCommand, Response<CurrentUserResponse>>




    {

        #region Fildes
        private readonly LocalizationService localize;
        private readonly ISellerService sellerService ;
        private readonly IMapper mapper;
        private readonly IAuthorizationService authorizationService ;
        private readonly IUserService userService;
        private readonly UserManager<Data.Entities.Identity.User> userManager;
        private readonly ICurrentUserService currentUserService;
        private readonly IInstructorsService instructorsService;

        #endregion

        #region constructor

        public SellerCommandHandler(LocalizationService localization
            , ISellerService sellerService,
        IMapper mapper,IAuthorizationService authorizationService
            , UserManager<Data.Entities.Identity.User> userManager
            , IUserService userService
            , ICurrentUserService currentUserService
            ,IInstructorsService instructorsService) : base(localization)
        {
            this.localize = localization;
            this.sellerService = sellerService;
            this.mapper = mapper;
            this.authorizationService = authorizationService;
            this.userService = userService;
            this.userManager = userManager;
            this.currentUserService = currentUserService;
            this.instructorsService = instructorsService;
        }


        #endregion

        #region Methods

        public async Task<Response<string>> Handle(AddSellerCommand request, CancellationToken cancellationToken)
    {
            var user = mapper.Map<Data.Entities.Identity.User>(request);

            var userResult = await userService.AddUserAsync(user, request.Password);

            switch (userResult.Item1)
            {

                case "EmailOrPhoneIsExist":
                    return BadRequest<string>(localize.Get("EmailOrPhoneIsExist"));
                    break;
                case "UserNameIsExist":
                    return BadRequest<string>(localize.Get("UserNameIsExist"));
                    break;
                case "Add_Correct_info":
                    return BadRequest<string>(localize.Get("Add_Correct_info"));
                    break;
                case "Failed":
                    return BadRequest<string>(localize.Get("TryToRegisterAgain"));

                    break;

                case "Success":
                    break;

                default:
                    return BadRequest<string>(userResult.Item1);
            }

            var userId = userResult.Item2.Value;

            var seller = mapper.Map<Sellers>(request);

            var result = await sellerService.AddSellerAsync(seller, userId);
            if (result.Item1 != "Success")
            {
                return BadRequest<string>(localize.Get(result.Item1));
            }
            else
            {
                return Success<string>("Success", meta: (result.Item2));
            }

        }



        // Update Seller  

        public async Task<Response<string>> Handle(UpdateSellerCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUserService.GetUserId();
            var user = userManager.Users.IgnoreQueryFilters()
                .FirstOrDefault(x => x.Id == userId);

            if (user == null)
                return NotFound<string>(localize.Get("UserNotFound"));


            if (user.IsDeleted)
                return NotFound<string>(localize.Get("SellerLinkedToDeletedUser"));


            var seller = await sellerService.GetTableAsTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (seller == null)
                return NotFound<string>(localize.Get("SellerNotFound"));

            if (!seller.IsActive)
                return NotFound<string>(localize.Get("SellerNotActive"));



            seller.BusinessType = request.BusinessType;
            seller.BusinessName = request.BusinessName;
           
            
            await sellerService.SaveChangesAsync();

            return Success<string>(localize.Get("SellerUpdatedSuccessfully"));
        }


        // Dis Active Seller  

        public async Task<Response<string>> Handle(DisActiveSellerCommand request, CancellationToken cancellationToken)
        {
            
            var seller = await sellerService.GetTableAsTracking()
                .FirstOrDefaultAsync(i => i.Id == request.Id);

            if (seller == null)
                return BadRequest<string>(localize.Get("SellerNotFound"));

            if (!seller.IsActive)
                return BadRequest<string>(localize.Get("SellerIsAlreadyNotActive"));


            seller.IsActive = false;


            await sellerService.SaveChangesAsync();

            return Success<string>(localize.Get("SellerDisActiveSuccessfully"));
        }

        // Active Seller  

        public async Task<Response<string>> Handle(ActiveSellerCommand request, CancellationToken cancellationToken)
        {
            

            var seller = await sellerService.GetTableAsTracking().IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.Id == request.Id);

            if (seller == null)
                return BadRequest<string>(localize.Get("SellerNotFound"));

            if (seller.IsActive)
                return BadRequest<string>(localize.Get("SellerIsAlreadyActive"));


            seller.IsActive = true;


            await sellerService.SaveChangesAsync();

            return Success<string>(localize.Get("SellerActiveSuccessfully"));
        }



    

            public async Task<Response<CurrentUserResponse>> Handle(GetUserByTokenCommand request,CancellationToken cancellationToken)
            {
                    int? sellerId = null;
                    int? instructorId = null;
            var userId = currentUserService.GetUserId();

           
                var user = await userManager.Users
                                .FirstOrDefaultAsync(x => x.Id == userId);

                if (user == null)
                return NotFound<CurrentUserResponse>(localize.Get("UserNotFound"));


            var role = (await userManager.GetRolesAsync(user)).FirstOrDefault();

            if (role == RolesHelper.Seller)
            {
                var seller = await sellerService.GetTableNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);

                if (seller == null)
                    return NotFound<CurrentUserResponse>(localize.Get("SellerNotFound"));
                sellerId = seller.Id;
            }


            if (role == RolesHelper.Instructor)
            {
                var instructor = await instructorsService.GetTableNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);

                if (instructor == null)
                    return NotFound<CurrentUserResponse>(localize.Get("InstructorNotFound"));

                instructorId = instructor.Id;

            }

            var result = new CurrentUserResponse
            {
                UserId = user.Id,
                Role = role,
                SellerId = sellerId,
                InstructorId = instructorId
            };

            return Success(result);
        }
        


        #endregion
    }
}
