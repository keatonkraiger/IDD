﻿using System;
using System.Threading.Tasks;
using NUnit.Framework;
using AdminUI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Common.Models;
using Common.Data;

namespace AdminUITest
{
    [TestFixture]
    public class SubmissionControllerTests
    {
        private readonly SubmissionContext _scontext;
        private readonly ILogger<SubmissionController> _logger;
        
        public SubmissionControllerTests()
        {
            _scontext = new InMemoryDbContextFactory().GetSubmissionContext();
            _logger = new NullLogger<SubmissionController>();
            _scontext.Add(new Timesheet{ 
                ClientName = "client",
                ClientPrime = "prime",
                ProviderName = "provider",
                ProviderId = "id",
                TotalHours = 34.3,
                ServiceGoal = "To help her eat cheese",
                ProgressNotes = "She ate the cheese",
                FormType = "OR526 Attendant Care",
                Submitted = DateTime.Parse("4/2/20 2:03PM"),
                RejectionReason = "",
                Status = "Pending",
                UriString = "hi,hello,how are you",
                LockInfo = new Lock
                {
                    User = "steve",
                    LastActivity = DateTime.Now
                }
                }
            );
            _scontext.Add(new Timesheet{ 
                ClientName = "client",
                ClientPrime = "prime",
                ProviderName = "provider",
                ProviderId = "id",
                TotalHours = 34.3,
                ServiceGoal = "To help her eat cheese",
                ProgressNotes = "She ate the cheese",
                FormType = "OR526 Attendant Care",
                Submitted = DateTime.Parse("4/2/20 2:03PM"),
                RejectionReason = "",
                Status = "Pending",
                UriString = "hi,hello,how are you"
                }
            );
            _scontext.SaveChanges();
        }

        [Test]
        public async Task ModalProcess_SubmissionNotFound()
        {
            var tc = new SubmissionController(_logger, _scontext);
            var result = await tc.Process(-1, "Accept", "Nah", "id", "04/01/2020", "04/15/2020", "pName", "cName", "PRIME", "timesheet", 1, "providerId","true");
            Assert.IsInstanceOf(typeof(NotFoundResult), result);
        }
        [Test]
        public async Task ModalProcess_LockNotFound()
        { 
            var tc = new SubmissionController(_logger, _scontext);
            var result = await tc.Process(2, "Accept", "Nah", "id", "04/01/2020", "04/15/2020", "pName", "cName", "PRIME", "timesheet", 1, "providerId","true") as ViewResult;
            Assert.AreEqual("NoPermission", result.ViewName);
        }
        [Test]
        public async Task ModalProcess_LockDoesntMatchUser()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "bob"),
                new Claim(ClaimTypes.Name, "bob")
            }));

            var tc = new SubmissionController(_logger, _scontext)
            {
                ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                }
            };

            var result = await tc.Process(1, "Accept", "Nah", "id", "04/01/2020", "04/15/2020", "pName", "cName", "PRIME", "timesheet", 1, "providerId","true") as ViewResult;
            Assert.AreEqual("NoPermission", result.ViewName);
        }
        [Test]
        public async Task ModalProcess_AllGood()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "steve"),
                new Claim(ClaimTypes.Name, "steve")
            }));

            var tc = new SubmissionController(_logger, _scontext)
            {
                ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                }
            };

            var result = await tc.Process(1, "Accept", "Nah", "id", "04/01/2020", "04/15/2020", "pName", "cName", "PRIME", "timesheet", 1, "providerId", "true") as RedirectToActionResult;
            Assert.AreEqual("Index", result.ActionName);
        }

    }
}
