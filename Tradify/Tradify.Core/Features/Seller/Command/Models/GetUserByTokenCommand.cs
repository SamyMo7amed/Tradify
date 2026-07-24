using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Tradify.Core.Bases;

namespace Tradify.Core.Features.Seller.Command.Models
{

    public class GetUserByTokenCommand : IRequest<Response<CurrentUserResponse>>
    {
    }

    public class CurrentUserResponse
    {
        public int UserId { get; set; }

        public string Role { get; set; }

        public int? SellerId { get; set; }

        public int? InstructorId { get; set; }
    }

}
