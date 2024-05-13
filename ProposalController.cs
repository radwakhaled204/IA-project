using jobconnect.Data;
using jobconnect.Models;
using jobconnect.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

//ProposalController about apply proposal by yasmin soliman and Radwa Khaled
namespace jobconnect.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProposalController : ControllerBase
    {
        private readonly IDataRepository<Proposal> _proposalRepository;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IDataRepository<Job> _jobRepository;

        public ProposalController(IDataRepository<Proposal> proposalRepository, IWebHostEnvironment hostingEnvironment, IDataRepository<Job> jobRepository)
        {
            _proposalRepository = proposalRepository;
            _hostingEnvironment = hostingEnvironment;
            _jobRepository = jobRepository;
        }
        /*********************************************************** Applyforjob **********************************************************/


        [HttpPost("Applyforjob")]
        public async Task<IActionResult> ApplyForJob([FromForm] ShowProposalDto proposalDto)
        {
            //check if valid model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // check if a CV file exists 
            if (proposalDto.CV_file == null || proposalDto.CV_file.Length == 0)
            {
                return BadRequest("CV file is required.");
            }

            // create a path to store the CV file
            var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "CV_files");
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(proposalDto.CV_file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await proposalDto.CV_file.CopyToAsync(stream);
            }

            var proposal = new Proposal
            {
                JobSeekerId = proposalDto.JobSeekerId,
                JobId = proposalDto.JobId,
                brief_description = proposalDto.brief_description,
                // store path
                CV_file = filePath
            };

            proposal.Proposal_date = DateTime.UtcNow;

            // save changes
            await _proposalRepository.AddAsync(proposal);
            await _proposalRepository.Save();

            var jobDto = await _jobRepository.GetByIdAsync(proposalDto.JobId);
            if (jobDto != null)
            {
                jobDto.No_of_proposal_submitted++;
                await _jobRepository.UpdateAsync(jobDto);
                await _jobRepository.Save();
            }

            return Ok("Proposal submitted successfully.");
        }

    }
}
