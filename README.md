# Employee & ID Card Management System

This is an **Employee Management & ID Card System** built with **C# .NET Core MVC**, **Entity Framework Core**, and **SQL Server**. The system allows HR/Admin to manage employees, register users, generate ID cards, and perform bulk uploads via Excel. It also includes **email OTP verification** for secure user registration.

---

## **Features**

### **1. User Management**
- Admin can register new users with roles: Admin, HR, Employee.
- Passwords are hashed for security.
- Email OTP verification for new user activation.
- Session-based authentication with role-based access control.

### **2. Employee Management**
- Create, Read, Update, Delete employee records.
- Fields include Name, Department, Designation, DOB, Contact Info, Blood Group, and optional image.
- Tracks who created the record (`CreatedBy`) and timestamps (`CardCreateDate`, `UpdatedAt`).
- Status flags for printing and email sending (`PrintedStatus`, `SentOnMailStatus`).

### **3. Bulk Upload**
- Admin can upload employees using an **Excel file**.
- Automatically maps Excel columns to database fields:


- Supports up to hundreds of records in one upload.

### **4. Email & OTP**
- Sends OTP to verify new users before account activation.
- Uses session to store OTP and pending email.
- Prevents duplicate email registration.

---

## **Tech Stack**

- **Backend:** C# .NET Core MVC
- **ORM:** Entity Framework Core
- **Database:** SQL Server
- **Frontend:** Razor Views, Bootstrap 5
- **Excel Processing:** ClosedXML
- **Email Service:** SMTP / EmailService
- **Authentication:** Session-based

---

## **Database Structure**

### **Users Table**
| Column        | Type       | Description                       |
|---------------|------------|-----------------------------------|
| UserId        | int (PK)   | Primary key                        |
| FullName      | string     | User full name                     |
| Email         | string     | Unique email                       |
| PasswordHash  | string     | Hashed password                    |
| Role          | string     | User role (Admin/HR/Employee)     |
| IsActive      | bool       | OTP verified                       |
| CreatedAt     | datetime   | Timestamp                          |

### **Employees Table**
| Column             | Type       | Description                       |
|--------------------|------------|-----------------------------------|
| EmployeeId         | int (PK)   | Primary key                        |
| Name               | string     | Employee name                      |
| FatherName         | string     | Father's name                      |
| MotherName         | string     | Mother's name                      |
| DOB                | datetime   | Date of birth                      |
| Department         | string     | Department                         |
| Designation        | string     | Job designation                     |
| DateOfJoining      | datetime   | Joining date                        |
| BloodGroup         | string     | Blood group                         |
| MobileNo           | string     | Mobile number                       |
| Email              | string     | Email address                       |
| EmergencyContact   | string     | Emergency contact                   |
| Image              | string     | Path/URL of image                   |
| CreatedBy          | int (FK)   | Linked to Users                     |
| CardCreateDate     | datetime   | ID card creation date               |
| PrintedStatus      | bool       | Card printed status                 |
| SentOnMailStatus   | bool       | Email sent status                   |
| UpdatedAt          | datetime   | Last update timestamp               |
| IsActive           | bool       | Active employee                     |

---

## **Installation**


