# iCard System - Navigation Guide

## 🏠 **Home Page**
- **URL**: `/` or `/Home/Index`
- **Purpose**: Main landing page with all available services
- **Features**: Quick stats, service categories, and operation buttons

## 📊 **Dashboard**
- **URL**: `/Dashboard/Index`
- **Purpose**: System overview with key statistics
- **Features**: 
  - 5 Key Indicators (Total Employees, Generated Cards, Active, Inactive, Emails Sent)
  - Quick Actions
  - All Operations organized by category

## 👥 **Employee Management**
- **URL**: `/Employees/Index`
- **Purpose**: Manage all employees
- **Features**:
  - View all employees with pagination (20, 50, 100 per page)
  - Search by name, mobile, ID
  - Filter by department, designation, date
  - Add new employee
  - Edit employee details
  - Bulk upload via Excel

## 🆔 **ID Card Operations**
- **URL**: `/Employees/Index` (with actions)
- **Purpose**: Generate and manage ID cards
- **Features**:
  - Generate PDF ID cards
  - Generate QR codes
  - Bulk download
  - Print cards
  - Preview cards
  - Card templates

## 📧 **Communication**
- **URL**: `/Employees/Index` (with actions)
- **Purpose**: Send and share ID cards
- **Features**:
  - Send via email
  - Share via WhatsApp
  - Bulk email sending
  - Email notifications
  - Email history
  - Email reports

## 👤 **User Management**
- **URL**: `/Users/Profile`, `/Users/ChangePassword`, etc.
- **Purpose**: Manage user accounts and profiles
- **Features**:
  - My Profile (view/edit)
  - Change Password
  - Register User (Admin only)
  - Manage Users (Admin only)
  - User Permissions (Admin only)
  - User Roles (Admin only)

## 📈 **Analytics**
- **URL**: `/Dashboard/Index` and related actions
- **Purpose**: System statistics and reports
- **Features**:
  - System statistics
  - Database status
  - Server status
  - Export data
  - Import data
  - Backup system

## 🔐 **Authentication**
- **Login**: `/Users/Login`
- **Register**: `/Users/Register` (Admin only)
- **Forgot Password**: `/Users/ForgotPassword`
- **Reset Password**: `/Users/ResetPassword`
- **Verify OTP**: `/Users/VerifyOTP`
- **Logout**: `/Dashboard/Logout`

## 📱 **Navigation Structure**

### **Header Navigation (Always Visible)**
1. **Home** - Main landing page
2. **Dashboard** - System overview and statistics
3. **Employees** - Employee management operations
4. **ID Cards** - ID card generation and management
5. **Communication** - Email and sharing operations
6. **Analytics** - System analytics and reports
7. **User Menu** - Profile, password, admin functions

### **Quick Access Points**
- **Home Page**: All services organized by category
- **Dashboard**: Key indicators and quick actions
- **Header Menu**: Dropdown menus for each category

## 🎯 **Key Features by Role**

### **All Users**
- View Dashboard
- Manage own profile
- Change password
- View employees
- Generate ID cards
- Send emails

### **Admin Users (Additional)**
- Register new users
- Manage all users
- User permissions
- User roles
- System administration

## 🚀 **Getting Started**

1. **Login** at `/Users/Login`
2. **View Dashboard** at `/Dashboard/Index` for overview
3. **Add Employees** at `/Employees/Create`
4. **Generate ID Cards** from employee list
5. **Send Cards** via email or WhatsApp
6. **Manage Profile** at `/Users/Profile`

## 📋 **Common Workflows**

### **Adding New Employee**
1. Go to Employees → Add New Employee
2. Fill in employee details
3. Upload photo
4. Save employee

### **Generating ID Card**
1. Go to Employees → View All Employees
2. Click "Generate ID Card" for specific employee
3. Choose to send via email or just generate

### **Bulk Operations**
1. Go to Employees → Bulk Upload
2. Upload Excel file with employee data
3. Process bulk upload
4. Generate cards for all employees

### **System Administration**
1. Go to User Menu → Register User (Admin only)
2. Set user role and permissions
3. Manage system settings
4. View analytics and reports
