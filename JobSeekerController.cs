using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using jobconnect.Dtos;
using jobconnect.Models;
using jobconnect.Data;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;

//JobSeekerController about Job Seekers by Radwa Khaled

namespace jobconnect.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobSeekerController : Controller
    {
        private readonly IDataRepository<User> _userRepository;
        private readonly IDataRepository<Job> _jobRepository;
        private readonly IDataRepository<SavedJobs> _savedJobsRepository;
 


        public JobSeekerController(IDataRepository<User> userRepository, IDataRepository<Job> jobRepository,IDataRepository<SavedJobs> savedJobsRepository)
        {
            _userRepository = userRepository;
            _jobRepository = jobRepository;
            _savedJobsRepository = savedJobsRepository;

        }
        /*********************************************************** Getalljobs **********************************************************/
        [HttpGet("Getalljobs")]
        public async Task<IActionResult> GetAllJobs()
        {
            var jobs = await _jobRepository.GetAllAsync();
            var jobDtos = new List<JobDto>();
            //loop in job table
            foreach (var job in jobs)
            {
                //check id Accepted_by_admin to view
                var employer = await _userRepository.GetByIdAsync(job.EmpId);
                if (employer != null && job.Accepted_by_admin) 
                {
                    jobDtos.Add(new JobDto
                    {
                        Job_title = job.Job_title,
                        Job_description = job.Job_description,
                        Job_type = job.Job_type,
                        Post_creation_date = job.Post_creation_date,
                        location = job.location,
                        industry = job.industry,
                        salary_budget = job.salary_budget,
                        No_of_position_required = job.No_of_position_required,
                        EmpId = job.EmpId,
                        EmployerName = employer.Username,
                        Accepted_by_admin = job.Accepted_by_admin 
                    });
                }
            }

            return Ok(jobDtos);
        }


        /*********************************************************** Getjobbyid **********************************************************/

        [HttpGet("Getjobsbyid")]
        public async Task<IActionResult> GetJobsByid(int jobId)
        {


            var job = await _jobRepository.GetByIdAsync(jobId);

            if (job == null)
            {
                return NotFound("Job not found");
            }

            var jobDto = new JobDto
            {
                Job_title = job.Job_title,
                Job_description = job.Job_description,
                Job_type = job.Job_type,
                Post_creation_date = job.Post_creation_date,
                location = job.location,
                industry = job.industry,
                salary_budget = job.salary_budget,
                No_of_position_required = job.No_of_position_required,
                EmpId = job.EmpId,
                Accepted_by_admin = job.Accepted_by_admin
            };

            return Ok(jobDto);
        }

        /*********************************************************** SaveJob **********************************************************/
       // [Authorize(Roles = "JobSeeker")]
        [HttpPost("SaveJob")]
        public async Task<IActionResult> SaveJob(SaveJobDto saveJobDto)
        {                           
                var jobSeeker = await _userRepository.GetByIdAsync(saveJobDto.JobSeekerId);
                var job = await _jobRepository.GetByIdAsync(saveJobDto.JobId);

                if (jobSeeker == null || job == null)
                {
                    return NotFound("User or Job not found");
                }

                var savedJob = new SavedJobs
                {
                    JobSeekerId = saveJobDto.JobSeekerId,
                    JobId = saveJobDto.JobId
                };
             
                await _savedJobsRepository.AddAsync(savedJob);
                await _savedJobsRepository.Save();

                return Ok("Job saved successfully");            
        }

        /*********************************************************** UnsavedJob **********************************************************/
       // [Authorize(Roles = "JobSeeker")]
        [HttpPost("UnsavedJob")]
        public async Task<IActionResult> UnsavedJob(SaveJobDto saveJobDto)
        {
            
            var savedJob = await _savedJobsRepository.GetSingleAsync(x => x.JobSeekerId == saveJobDto.JobSeekerId && x.JobId == saveJobDto.JobId);

           
            if (savedJob == null)
            {
                return NotFound("Job not saved by this user");
            }

            
            _savedJobsRepository.DeleteAsync(savedJob);
            await _savedJobsRepository.Save();

            return Ok("Job unsaved successfully");
        }


        /*********************************************************** GetAllSavedJobs **********************************************************/
        //[Authorize(Roles = "JobSeeker")]
        [HttpGet("GetAllSavedJobs/{jobSeekerId}")]
        public async Task<IActionResult> GetAllSavedJobs(int jobSeekerId)
        {
       
            var allSavedJobs = await _savedJobsRepository.GetAllAsync();
           
            var savedJobs = allSavedJobs.Where(x => x.JobSeekerId == jobSeekerId);
        
            if (savedJobs == null || !savedJobs.Any())
            {
                return NotFound("No saved jobs found for this user");
            }
   
            var savedJobIds = savedJobs.Select(x => x.JobId).ToList();
            return Ok(savedJobIds);
        }






    }
}
