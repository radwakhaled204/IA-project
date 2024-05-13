using jobconnect.Data;
using jobconnect.Dtos;
using jobconnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

//EmployerController about employers AND Post new jobs  posts by Radwa Khaled
namespace jobconnect.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployerController : Controller
    {
        private readonly IDataRepository<Job> _jobRepository;
        private readonly IDataRepository<Proposal> _proposalRepository;
        private readonly IDataRepository<JobSeeker> _jobSeekerRepository;
        public EmployerController(IDataRepository<Job> jobRepository, IDataRepository<Proposal> proposalRepository, IDataRepository<JobSeeker> jobSeekerRepository)
        {
            _proposalRepository = proposalRepository;
            _jobSeekerRepository = jobSeekerRepository;
            _jobRepository = jobRepository;
        }
        /*********************************************************** -CreateJobPost **********************************************************/
             //update
        [HttpPost("CreateJobPost")]
        public async Task<IActionResult> CreateJobPost(ShowJobDto jobDto) 
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var job = new Job
            {
                Job_title = jobDto.Job_title,
                Job_description = jobDto.Job_description,
                Job_type = jobDto.Job_type,
                location = jobDto.location,
                industry = jobDto.industry,
                salary_budget = jobDto.salary_budget,
                No_of_position_required = jobDto.No_of_position_required,
                EmpId = jobDto.EmpId,                        
            };
            job.Post_creation_date = DateTime.UtcNow;
            await _jobRepository.AddAsync(job);
            await _jobRepository.Save();

            return Ok(job);
        }
        /*********************************************************** Getallsubmittedproposals **********************************************************/

       // [Authorize(Roles = "Employer")]
        [HttpGet("Getallsubmittedproposals")]
        public async Task<IActionResult> GetAllProposals()
        {
            var proposals = await _proposalRepository.GetAllAsync();
            var proposalDtos = new List<ProposalDto>();

            foreach (var proposal in proposals)
            {
                
                var jobSeeker = await _jobSeekerRepository.GetByIdAsync(proposal.JobSeekerId);

                proposalDtos.Add(new ProposalDto
                {
                    JobId = proposal.JobId,
                    JobSeekerId = proposal.JobSeekerId,
                    Proposal_date = proposal.Proposal_date,
                    brief_description = proposal.brief_description,
                    CV_file = proposal.CV_file,
                    JobSeekerName = jobSeeker != null ? $"{jobSeeker.first_name} {jobSeeker.last_name}" : "Unknown"
                });
            }

            return Ok(proposalDtos);
        }
        /*********************************************************** AcceptProposal **********************************************************/
       // [Authorize(Roles = "Employer")]
        [HttpPost("AcceptProposal")]
        public async Task<IActionResult> AcceptProposal(int jobId, int jobSeekerId)
        {

               //search about proposal
                var proposal = await _proposalRepository.GetByIdAsync(jobId, jobSeekerId);
                if (proposal == null)
                {
                    return NotFound("Proposal not found.");
                }

                //delete proposal
                var proposals = await _proposalRepository.GetByJobIdAsync(jobId);
                foreach (var p in proposals)
                {
                    await _proposalRepository.DeleteAsync(p);
                }

                // save chanes ya radwa
                if (!await _proposalRepository.Save())
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, $"Error when saving changes.");
                }

                // search in job table about No_of_position_required
                var job = await _jobRepository.GetByIdAsync(proposal.JobId);
                if (job == null)
                {
                    return NotFound("Job not found");
                }

                //If accepted, decrease 1 from No_of_position_required
                job.No_of_position_required--;

                //if No_of_position_required = 0 , delete job

                if (job.No_of_position_required == 0)
                {
                    await _jobRepository.DeleteAsync(job);
                }
                else
                {
                    //update
                    await _jobRepository.UpdateAsync(job);
                }

                // save chanes ya radwa
                if (!await _jobRepository.Save())
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, $"Error saving changes to job.");
                }

                return Ok("Proposal Accepted successfully.");
            

        }
        /*********************************************************** RejectProposal **********************************************************/
        //[Authorize(Roles = "Employer")]
        [HttpPost("RejectProposal")]
        public async Task<IActionResult> RejectProposal(int jobId, int jobSeekerId)
      
        {
            //search about proposal

            var rejectpro = await _proposalRepository.GetByIdAsync(jobId, jobSeekerId);
            if (rejectpro == null)
            {
                return NotFound("Proposal not found");
            }
            // if accepted_by_emp = false , delete proposal
            rejectpro.accepted_by_emp = false;
            await _proposalRepository.UpdateAsync(rejectpro);
            await _proposalRepository.DeleteAsync(rejectpro);
            await _proposalRepository.Save();

            return Ok("Proposal Refused successfully");
        }


    }

}
