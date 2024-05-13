using jobconnect.Data;
using jobconnect.Dtos;
using jobconnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

//AdminController about Manage employers (CRUD) AND Accept or refuse job posts by Radwa Khaled
namespace jobconnect.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {

        private readonly IDataRepository<User> _userRepository;
        private readonly IDataRepository<Employer> _employerRepository;
        private readonly IDataRepository<Job> _jobRepository;
        private readonly IAuthRepository _authRepository;
        public AdminController(IDataRepository<User> userRepository, IDataRepository<Employer> employerRepository, IDataRepository<Job> jobRepository, IAuthRepository authRepository)
        {
            _userRepository = userRepository;
            _employerRepository = employerRepository;
            _jobRepository = jobRepository;
            _authRepository = authRepository;
        }
        /*********************************************************** CreateEmployer **********************************************************/
        //[Authorize(Roles = "Admin")]
        [HttpPost("CreateEmployer")]
        public async Task<IActionResult> CreateEmployer(EmployerDto employerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            byte[] passwordHash, passwordSalt;

            CreatePasswordHash(employerDto.Password, out passwordHash, out passwordSalt);


            var user = new User
            {
                Username = employerDto.Username,
                Email = employerDto.Email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                UserType = "Employer"
            };


            var employer = new Employer
            {
                Company_name = employerDto.Company_name,
                Company_description = employerDto.Company_description,
                mainaddress = employerDto.mainaddress,
                User = user
            };

            // add user and employer to the database
            await _userRepository.AddAsync(user);
            await _employerRepository.AddAsync(employer);

            //dont forget the save function again Radwaaaaaaaa
            await _employerRepository.Save();

            return Ok($"Employer created successfully. Username: {user.Username} , Password: {employerDto.Password}");
        }
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }


        /*********************************************************** GetAllEmployers **********************************************************/
        //[Authorize(Roles = "Admin")]
        [HttpGet("GetAllEmployers")]

        public async Task<IActionResult> GetAllEmployers()
        {
            IEnumerable<User> users = await _userRepository.GetAllAsync();
            IEnumerable<ShowEmployerDto> empFullDataList = Enumerable.Empty<ShowEmployerDto>();
            foreach (User user in users)
            {
                if(user.UserType == "Employer")
                {
                    ShowEmployerDto empFullData = new ShowEmployerDto();
                    empFullData.EmployerId = user.UserId;
                    empFullData.Username = user.Username;
                    empFullData.Email = user.Email;
                    Employer emp = await _employerRepository.GetByIdAsync(user.UserId);
                    empFullData.Company_name = emp.Company_name;
                    empFullData.Company_description = emp.Company_description;
                    empFullData.mainaddress = emp.mainaddress;
                    empFullDataList = empFullDataList.Append(empFullData);
                }   
            };
            return Ok(empFullDataList);
        }

        /*********************************************************** GetEmployerById **********************************************************/
        //[Authorize(Roles = "Admin")]
        [HttpGet("GetEmployerById/{id}")]
        public async Task<IActionResult> GetEmployerById(int id)
        {
            User user = await _userRepository.GetByIdAsync(id);
            Employer employer = await _employerRepository.GetByIdAsync(id);
            if (employer == null || user == null)
            {
                return NotFound();
            }
            ShowEmployerDto empFullData = new ShowEmployerDto();
            empFullData.EmployerId = user.UserId;
            empFullData.Username = user.Username;
            empFullData.Email = user.Email;
            empFullData.Company_name = employer.Company_name;
            empFullData.Company_description = employer.Company_description;
            empFullData.mainaddress = employer.mainaddress;
            return Ok(empFullData);
        }
        /*********************************************************** UpdateEmployerById **********************************************************/
        //[Authorize(Roles = "Admin")]
        [HttpPut("UpdateEmployer/{id}")]
        public async Task<IActionResult> UpdateEmployer(int id, EmployerDto employerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            User existinguser = await _userRepository.GetByIdAsync(id);
            Employer existingEmployer = await _employerRepository.GetByIdAsync(id);
            if (existingEmployer == null)
            {
                return NotFound();
            }

            employerDto.Email = employerDto.Email.ToLower();
            if (await _authRepository.UserExist(employerDto.Email))
            {
                return BadRequest("Email already exists");
            }
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(employerDto.Password, out passwordHash, out passwordSalt);

            existinguser.Email = employerDto.Email;
            existinguser.PasswordHash = passwordHash;
            existinguser.PasswordSalt = passwordSalt;
            existingEmployer.Company_name = employerDto.Company_name;
            existingEmployer.Company_description = employerDto.Company_description;
            existingEmployer.mainaddress = employerDto.mainaddress;


            // update employer in the database
            await _employerRepository.UpdateAsync(existingEmployer);
                await _employerRepository.Save();

                return Ok("Employer updated successfully.");
        }
        /*********************************************************** DeleteEmployerById **********************************************************/

        //[Authorize(Roles = "Admin")]
        [HttpDelete("DeleteEmployer/{id}")]
        public async Task<IActionResult> DeleteEmployer(int id)
        {
            var existingEmployer = await _employerRepository.GetByIdAsync(id);
            User existinguser = await _userRepository.GetByIdAsync(id);
            if (existingEmployer == null)
            {
                return NotFound();
            }
     
            // delete employer from the database
            await _employerRepository.DeleteAsync(existingEmployer);
            await _userRepository.DeleteAsync(existinguser);
            await _employerRepository.Save();

            return Ok("Employer deleted successfully.");
        }
        /*********************************************************** GetAllJobs **********************************************************/

        [HttpGet("GetAllJobs")]
        public async Task<IActionResult> GetAllJobs()
        {
            var jobs = await _jobRepository.GetAllAsync();
            var jobDtos = new List<JobDto>();


            foreach (var job in jobs)
            {
                var employer = await _userRepository.GetByIdAsync(job.EmpId);
                jobDtos.Add(new JobDto
                {
                    Job_title = job.Job_title,
                    Job_description = job.Job_description,
                    Job_type = job.Job_type,
                    location = job.location,
                    industry = job.industry,
                    salary_budget = job.salary_budget,
                    No_of_proposal_submitted = job.No_of_proposal_submitted,
                    No_of_position_required = job.No_of_position_required,
                    Accepted_by_admin = job.Accepted_by_admin,
                    Post_creation_date = job.Post_creation_date,
                    EmpId = job.EmpId,
                    EmployerName = employer.Username,
                });
            }

            return Ok(jobDtos);
        }
        /*********************************************************** Accept job posts  **********************************************************/
        //[Authorize(Roles = "Admin")]
        [HttpPost("accept-job/{jobId}")]
        public async Task<IActionResult> AcceptJob(int jobId)
        {

                var job = await _jobRepository.GetByIdAsync(jobId);
                if (job == null)
                {
                    return NotFound("Job not found");
                }

                job.Accepted_by_admin = true;
                await _jobRepository.UpdateAsync(job);
                await _jobRepository.Save();

                return Ok("Job accepted successfully");          
        }
        /*********************************************************** refuse job posts  **********************************************************/
        //[Authorize(Roles = "Admin")]
        [HttpPost("refuse-job/{jobId}")]
        public async Task<IActionResult> RefuseJob(int jobId)
        {
                var job = await _jobRepository.GetByIdAsync(jobId);
                if (job == null)
                {
                    return NotFound("Job not found");
                }

                job.Accepted_by_admin = false;  
                 await _jobRepository.UpdateAsync(job);
                 await _jobRepository.DeleteAsync(job);
                 await _jobRepository.Save();

                return Ok("Job Refused successfully");
        }




    }
}
