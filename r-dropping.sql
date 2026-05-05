-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: May 05, 2026 at 01:36 PM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.0.30

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `r-dropping`
--

-- --------------------------------------------------------

--
-- Table structure for table `buyer`
--

CREATE TABLE `buyer` (
  `buyer_id` int(11) NOT NULL,
  `first_name` varchar(20) NOT NULL,
  `last_name` varchar(20) NOT NULL,
  `contact_no` varchar(15) DEFAULT NULL,
  `address` varchar(25) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `buyer`
--

INSERT INTO `buyer` (`buyer_id`, `first_name`, `last_name`, `contact_no`, `address`, `created_at`) VALUES
(1, 'test123', 'test', NULL, NULL, '2026-04-25 10:35:41'),
(3, 'Test123', 'Test123', '09946826707', NULL, '2026-04-25 14:48:36'),
(4, 'test123', 'test', '123123123', NULL, '2026-05-03 23:08:59');

-- --------------------------------------------------------

--
-- Table structure for table `courier`
--

CREATE TABLE `courier` (
  `courier_id` int(11) NOT NULL,
  `first_name` varchar(20) NOT NULL,
  `last_name` varchar(20) NOT NULL,
  `vehicle_type` varchar(15) NOT NULL,
  `vehicle_brand` varchar(15) NOT NULL,
  `plate_no` varchar(10) NOT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `courier`
--

INSERT INTO `courier` (`courier_id`, `first_name`, `last_name`, `vehicle_type`, `vehicle_brand`, `plate_no`, `created_at`) VALUES
(3, 'test123', 'test123', 'Motorcyle', 'Honda', '123123', '2026-04-26 02:37:09'),
(4, 'Test', 'Test123', 'Motorcyle', 'Suzuki', '123123', '2026-05-02 04:15:47');

-- --------------------------------------------------------

--
-- Table structure for table `delivery`
--

CREATE TABLE `delivery` (
  `item_id` int(11) NOT NULL,
  `courier_id` int(11) NOT NULL,
  `shipping_fee` float NOT NULL,
  `datetime_delivered` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `employee`
--

CREATE TABLE `employee` (
  `employee_id` int(11) NOT NULL,
  `first_name` varchar(20) NOT NULL,
  `last_name` varchar(20) NOT NULL,
  `middle_name` varchar(20) DEFAULT NULL,
  `position` varchar(20) NOT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `employee`
--

INSERT INTO `employee` (`employee_id`, `first_name`, `last_name`, `middle_name`, `position`, `created_at`) VALUES
(4, 'Godwin', 'Galvez', 'Test', 'Admin', '2026-04-22 23:33:41'),
(9, 'Johm Mark', 'Pardo', NULL, 'Manager', '2026-04-25 14:18:27'),
(11, 'test123', 'test', 'test', 'Admin', '2026-05-03 23:08:18');

-- --------------------------------------------------------

--
-- Table structure for table `item`
--

CREATE TABLE `item` (
  `item_id` int(11) NOT NULL,
  `managed_by` int(11) NOT NULL,
  `seller_id` int(11) NOT NULL,
  `pricing_id` int(11) NOT NULL,
  `buyer_id` int(11) NOT NULL,
  `item_name` varchar(25) NOT NULL,
  `description` varchar(50) DEFAULT NULL,
  `image_path` varchar(100) NOT NULL,
  `drop_off_date` datetime NOT NULL DEFAULT current_timestamp(),
  `pickup_date` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `item`
--

INSERT INTO `item` (`item_id`, `managed_by`, `seller_id`, `pricing_id`, `buyer_id`, `item_name`, `description`, `image_path`, `drop_off_date`, `pickup_date`) VALUES
(4, 4, 4, 1, 1, '0', '', 'images\\items\\item_20260505_192736_1638.png', '2026-05-05 19:27:39', NULL),
(5, 4, 4, 1, 1, '0', '', 'images\\items\\item_20260505_192736_1638.png', '2026-05-05 19:28:02', NULL),
(6, 4, 4, 1, 1, '0', '', 'images\\items\\item_20260505_193043_2084.png', '2026-05-05 19:30:45', NULL),
(7, 9, 4, 1, 3, '0', NULL, 'images\\items\\item_20260505_193443_2167.png', '2026-05-05 19:35:10', NULL);

-- --------------------------------------------------------

--
-- Table structure for table `pricing`
--

CREATE TABLE `pricing` (
  `pricing_id` int(11) NOT NULL,
  `rate_label` varchar(25) NOT NULL,
  `description` varchar(50) DEFAULT NULL,
  `base_fee` float NOT NULL,
  `daily_increment_fee` float NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `pricing`
--

INSERT INTO `pricing` (`pricing_id`, `rate_label`, `description`, `base_fee`, `daily_increment_fee`) VALUES
(1, 'Bags', 'For small items like bags', 12.5, 6);

-- --------------------------------------------------------

--
-- Table structure for table `seller`
--

CREATE TABLE `seller` (
  `seller_id` int(11) NOT NULL,
  `seller_name` varchar(25) NOT NULL,
  `email` varchar(25) DEFAULT NULL,
  `contact_no` varchar(20) DEFAULT NULL,
  `platform` varchar(20) NOT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `seller`
--

INSERT INTO `seller` (`seller_id`, `seller_name`, `email`, `contact_no`, `platform`, `created_at`) VALUES
(4, '123', '123@email.com', NULL, 'TikTok', '2026-05-03 23:09:28'),
(5, 'Seller test', 'Seller@email.com', NULL, 'Facebook', '2026-05-04 18:46:14');

-- --------------------------------------------------------

--
-- Table structure for table `storage_unit`
--

CREATE TABLE `storage_unit` (
  `storage_unit_id` int(11) NOT NULL,
  `storage_name` varchar(25) NOT NULL,
  `storage_type` varchar(25) NOT NULL,
  `capacity_limit` smallint(6) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `storage_unit`
--

INSERT INTO `storage_unit` (`storage_unit_id`, `storage_name`, `storage_type`, `capacity_limit`) VALUES
(1, 'Bags and Clothes', 'Cabinet', 15),
(2, 'test', 'Basket', 5),
(4, 'Test12345', 'Basket', 2);

-- --------------------------------------------------------

--
-- Table structure for table `stored_on`
--

CREATE TABLE `stored_on` (
  `storage_unit_id` int(11) NOT NULL,
  `item_id` int(11) NOT NULL,
  `date_stored` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `stored_on`
--

INSERT INTO `stored_on` (`storage_unit_id`, `item_id`, `date_stored`) VALUES
(1, 6, '2026-05-05 19:30:45'),
(1, 7, '2026-05-05 19:35:10');

--
-- Indexes for dumped tables
--

--
-- Indexes for table `buyer`
--
ALTER TABLE `buyer`
  ADD PRIMARY KEY (`buyer_id`);

--
-- Indexes for table `courier`
--
ALTER TABLE `courier`
  ADD PRIMARY KEY (`courier_id`);

--
-- Indexes for table `delivery`
--
ALTER TABLE `delivery`
  ADD KEY `item_id` (`item_id`),
  ADD KEY `courier_id` (`courier_id`);

--
-- Indexes for table `employee`
--
ALTER TABLE `employee`
  ADD PRIMARY KEY (`employee_id`);

--
-- Indexes for table `item`
--
ALTER TABLE `item`
  ADD PRIMARY KEY (`item_id`),
  ADD KEY `buyer_id` (`buyer_id`),
  ADD KEY `managed_by` (`managed_by`),
  ADD KEY `pricing_id` (`pricing_id`),
  ADD KEY `seller_id` (`seller_id`);

--
-- Indexes for table `pricing`
--
ALTER TABLE `pricing`
  ADD PRIMARY KEY (`pricing_id`);

--
-- Indexes for table `seller`
--
ALTER TABLE `seller`
  ADD PRIMARY KEY (`seller_id`);

--
-- Indexes for table `storage_unit`
--
ALTER TABLE `storage_unit`
  ADD PRIMARY KEY (`storage_unit_id`);

--
-- Indexes for table `stored_on`
--
ALTER TABLE `stored_on`
  ADD KEY `item_id` (`item_id`),
  ADD KEY `storage_unit_id` (`storage_unit_id`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `buyer`
--
ALTER TABLE `buyer`
  MODIFY `buyer_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT for table `courier`
--
ALTER TABLE `courier`
  MODIFY `courier_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT for table `employee`
--
ALTER TABLE `employee`
  MODIFY `employee_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=13;

--
-- AUTO_INCREMENT for table `item`
--
ALTER TABLE `item`
  MODIFY `item_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=8;

--
-- AUTO_INCREMENT for table `pricing`
--
ALTER TABLE `pricing`
  MODIFY `pricing_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT for table `seller`
--
ALTER TABLE `seller`
  MODIFY `seller_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT for table `storage_unit`
--
ALTER TABLE `storage_unit`
  MODIFY `storage_unit_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `delivery`
--
ALTER TABLE `delivery`
  ADD CONSTRAINT `delivery_ibfk_1` FOREIGN KEY (`item_id`) REFERENCES `courier` (`courier_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `delivery_ibfk_2` FOREIGN KEY (`courier_id`) REFERENCES `item` (`item_id`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `item`
--
ALTER TABLE `item`
  ADD CONSTRAINT `item_ibfk_1` FOREIGN KEY (`buyer_id`) REFERENCES `buyer` (`buyer_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `item_ibfk_2` FOREIGN KEY (`managed_by`) REFERENCES `employee` (`employee_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `item_ibfk_3` FOREIGN KEY (`pricing_id`) REFERENCES `pricing` (`pricing_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `item_ibfk_4` FOREIGN KEY (`seller_id`) REFERENCES `seller` (`seller_id`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `stored_on`
--
ALTER TABLE `stored_on`
  ADD CONSTRAINT `stored_on_ibfk_1` FOREIGN KEY (`item_id`) REFERENCES `item` (`item_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `stored_on_ibfk_2` FOREIGN KEY (`storage_unit_id`) REFERENCES `storage_unit` (`storage_unit_id`) ON DELETE CASCADE ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
