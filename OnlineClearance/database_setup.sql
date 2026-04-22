-- ============================================================
-- Online Clearance System - Full Database Schema
-- Database: smileyface_OnlineClearance
-- Run this on your MySQL server (johnny.heliohost.org)
-- ============================================================

SET FOREIGN_KEY_CHECKS = 0;

-- ─── USERS ────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `users` (
  `id`                INT          NOT NULL AUTO_INCREMENT,
  `username`          VARCHAR(100) NOT NULL UNIQUE,
  `password`          VARCHAR(255) NOT NULL,
  `first_name`        VARCHAR(100) NOT NULL,
  `last_name`         VARCHAR(100) NOT NULL,
  `middle_initial`    VARCHAR(5)   NULL,
  `suffix_name`       VARCHAR(20)  NULL,
  `e_signature_path`  VARCHAR(500) NULL,
  `role`              VARCHAR(20)  NOT NULL DEFAULT 'student',
  `is_active`         TINYINT(1)   NOT NULL DEFAULT 1,
  `created_at`        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ─── COURSES ──────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `courses` (
  `id`          INT          NOT NULL AUTO_INCREMENT,
  `course_code` VARCHAR(20)  NOT NULL UNIQUE,
  `description` VARCHAR(200) NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ─── CURRICULUM ───────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `curriculum` (
  `id`          INT         NOT NULL AUTO_INCREMENT,
  `course_id`   INT         NOT NULL,
  `year_level`  INT         NOT NULL,
  `section`     VARCHAR(20) NOT NULL,
  PRIMARY KEY (`id`),
  CONSTRAINT `fk_curriculum_course` FOREIGN KEY (`course_id`) REFERENCES `courses`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ─── STUDENTS ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `students` (
  `id`             INT         NOT NULL AUTO_INCREMENT,
  `user_id`        INT         NOT NULL UNIQUE,
  `student_number` VARCHAR(20) NOT NULL UNIQUE,
  `curriculum_id`  INT         NOT NULL,
  `status`         VARCHAR(20) NOT NULL DEFAULT 'Regular',
  PRIMARY KEY (`id`),
  CONSTRAINT `fk_student_user`       FOREIGN KEY (`user_id`)       REFERENCES `users`(`id`)       ON DELETE CASCADE,
  CONSTRAINT `fk_student_curriculum` FOREIGN KEY (`curriculum_id`) REFERENCES `curriculum`(`id`)  ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ─── SIGNATORIES ──────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `signatories` (
  `id`                      INT         NOT NULL AUTO_INCREMENT,
  `user_id`                 INT         NOT NULL UNIQUE,
  `employee_id`             VARCHAR(30) NOT NULL UNIQUE,
  `uploaded_signature_path` VARCHAR(500) NULL,
  PRIMARY KEY (`id`),
  CONSTRAINT `fk_signatory_user` FOREIGN KEY (`user_id`) REFERENCES `users`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ─── ACADEMIC PERIODS ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `academic_periods` (
  `id`            INT         NOT NULL AUTO_INCREMENT,
  `academic_year` VARCHAR(20) NOT NULL,
  `semester`      VARCHAR(30) NOT NULL,
  `is_active`     TINYINT(1)  NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ─── SUBJECTS ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `subjects` (
  `id`           INT          NOT NULL AUTO_INCREMENT,
  `subject_code` VARCHAR(30)  NOT NULL UNIQUE,
  `title`        VARCHAR(200) NOT NULL,
  `lec_units`    INT          NOT NULL DEFAULT 0,
  `lab_units`    INT          NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ─── SUBJECT OFFERINGS ────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `subject_offerings` (
  `id`            INT         NOT NULL AUTO_INCREMENT,
  `mis_code`      VARCHAR(30) NOT NULL UNIQUE,
  `subject_code`  VARCHAR(30) NOT NULL,
  `instructor_id` INT         NOT NULL,
  `period_id`     INT         NOT NULL,
  PRIMARY KEY (`id`),
  CONSTRAINT `fk_offering_instructor` FOREIGN KEY (`instructor_id`) REFERENCES `signatories`(`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_offering_period`     FOREIGN KEY (`period_id`)     REFERENCES `academic_periods`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ─── ORGANIZATIONS ────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `organizations` (
  `id`             INT          NOT NULL AUTO_INCREMENT,
  `org_name`       VARCHAR(200) NOT NULL,
  `signatory_id`   INT          NOT NULL,
  `position_title` VARCHAR(100) NOT NULL,
  `curriculum_id`  INT          NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_org_position` (`org_name`, `position_title`),
  CONSTRAINT `fk_org_signatory`  FOREIGN KEY (`signatory_id`)  REFERENCES `signatories`(`id`)  ON DELETE RESTRICT,
  CONSTRAINT `fk_org_curriculum` FOREIGN KEY (`curriculum_id`) REFERENCES `curriculum`(`id`)   ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ─── STATUS TABLE ─────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `status_table` (
  `id`    INT         NOT NULL AUTO_INCREMENT,
  `label` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ─── CLEARANCE SUBJECTS ───────────────────────────────────────
CREATE TABLE IF NOT EXISTS `clearance_subjects` (
  `id`                  INT      NOT NULL AUTO_INCREMENT,
  `student_id`          INT      NOT NULL,
  `subject_offering_id` INT      NOT NULL,
  `status_id`           INT      NOT NULL DEFAULT 1,
  `remarks`             TEXT     NULL,
  `period_id`           INT      NOT NULL,
  `signed_at`           DATETIME NULL,
  `created_at`          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_cs_student_offering_period` (`student_id`, `subject_offering_id`, `period_id`),
  CONSTRAINT `fk_cs_student`  FOREIGN KEY (`student_id`)          REFERENCES `students`(`id`)          ON DELETE CASCADE,
  CONSTRAINT `fk_cs_offering` FOREIGN KEY (`subject_offering_id`) REFERENCES `subject_offerings`(`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_cs_status`   FOREIGN KEY (`status_id`)           REFERENCES `status_table`(`id`)      ON DELETE RESTRICT,
  CONSTRAINT `fk_cs_period`   FOREIGN KEY (`period_id`)           REFERENCES `academic_periods`(`id`)  ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ─── CLEARANCE ORGANIZATIONS ──────────────────────────────────
CREATE TABLE IF NOT EXISTS `clearance_organization` (
  `id`              INT      NOT NULL AUTO_INCREMENT,
  `student_id`      INT      NOT NULL,
  `organization_id` INT      NOT NULL,
  `period_id`       INT      NOT NULL,
  `status_id`       INT      NOT NULL DEFAULT 1,
  `remarks`         TEXT     NULL,
  `signed_at`       DATETIME NULL,
  `created_at`      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_co_student_org_period` (`student_id`, `organization_id`, `period_id`),
  CONSTRAINT `fk_co_student` FOREIGN KEY (`student_id`)      REFERENCES `students`(`id`)       ON DELETE CASCADE,
  CONSTRAINT `fk_co_org`     FOREIGN KEY (`organization_id`) REFERENCES `organizations`(`id`)  ON DELETE CASCADE,
  CONSTRAINT `fk_co_status`  FOREIGN KEY (`status_id`)       REFERENCES `status_table`(`id`)   ON DELETE RESTRICT,
  CONSTRAINT `fk_co_period`  FOREIGN KEY (`period_id`)       REFERENCES `academic_periods`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ─── ANNOUNCEMENTS ────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `announcements` (
  `id`         INT          NOT NULL AUTO_INCREMENT,
  `title`      VARCHAR(200) NOT NULL,
  `content`    TEXT         NOT NULL,
  `author_id`  INT          NOT NULL,
  `type`       VARCHAR(20)  NOT NULL DEFAULT 'General',
  `created_at` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  CONSTRAINT `fk_ann_author` FOREIGN KEY (`author_id`) REFERENCES `users`(`id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ─── EF MIGRATIONS TABLE ──────────────────────────────────────
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
  `MigrationId`    VARCHAR(150) NOT NULL,
  `ProductVersion` VARCHAR(32)  NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;

-- ─── SEED DATA ────────────────────────────────────────────────

-- Statuses
INSERT IGNORE INTO `status_table` (`id`, `label`) VALUES
  (1, 'Pending'),
  (2, 'Cleared'),
  (3, 'Rejected');

-- Default admin user (password: Admin@1234)
INSERT IGNORE INTO `users` (`username`, `password`, `first_name`, `last_name`, `role`, `is_active`)
VALUES (
  'admin',
  '$2a$11$rOzJqQ1K8Z9K9XGVz3L0DOqKqXkR7gN6H1S9M4F2T8P5Y6U3W0IEi',
  'System', 'Administrator', 'admin', 1
);

-- Sample courses
INSERT IGNORE INTO `courses` (`course_code`, `description`) VALUES
  ('BSIT', 'Bachelor of Science in Information Technology'),
  ('BSCS', 'Bachelor of Science in Computer Science'),
  ('BSED', 'Bachelor of Secondary Education'),
  ('BSBA', 'Bachelor of Science in Business Administration');

-- Sample curriculum entries
INSERT IGNORE INTO `curriculum` (`course_id`, `year_level`, `section`) VALUES
  (1, 1, 'A'), (1, 1, 'B'),
  (1, 2, 'A'), (1, 2, 'B'),
  (1, 3, 'A'),
  (1, 4, 'A'),
  (2, 1, 'A'), (2, 2, 'A'),
  (3, 1, 'A'), (4, 1, 'A');

-- Sample academic period
INSERT IGNORE INTO `academic_periods` (`academic_year`, `semester`, `is_active`) VALUES
  ('2024-2025', '1st Semester', 0),
  ('2024-2025', '2nd Semester', 1);

-- Sample subjects
INSERT IGNORE INTO `subjects` (`subject_code`, `title`, `lec_units`, `lab_units`) VALUES
  ('IT101', 'Introduction to Computing', 3, 0),
  ('IT102', 'Computer Programming 1', 2, 1),
  ('IT201', 'Data Structures', 3, 0),
  ('IT202', 'Database Management', 2, 1),
  ('IT301', 'Systems Analysis & Design', 3, 0),
  ('IT401', 'Capstone Project 1', 1, 5),
  ('GE101', 'Understanding the Self', 3, 0),
  ('GE102', 'Purposive Communication', 3, 0),
  ('MATH101', 'Mathematics in the Modern World', 3, 0),
  ('PE101', 'Physical Education 1', 2, 0);

SELECT 'Database initialized successfully!' AS message;
