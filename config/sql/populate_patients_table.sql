LOCK TABLES gpconnect.patients WRITE;

INSERT INTO gpconnect.patients
  (id,title,first_name,last_name,address_1,address_2,address_3,postcode,phone,date_of_birth,gender,nhs_number,pas_number,department_id,gp_id,lastUpdated,sensitive_flag)
VALUES
  (1,'MRS','Minnie','DAWES','24 GRAMMAR SCHOOL ROAD','BRIGG','','DN20 8AF','01454587554','1952-05-31','Female',9000000017,000001,1,3,'2016-07-25 12:00:00',FALSE),
  (2,'MRS','Minnie','DAWES','24 GRAMMAR SCHOOL ROAD','BRIGG','','DN20 8AF','01454587554','1952-05-31','Female',9476719931,000002,1,3,'2016-07-25 12:00:00',FALSE),
  (3,'MR','Julian','PHELAN','FARM HOUSE','BONNYHALE ROAD','S HUMBERSIDE','DN17 4JQ','02123636563','1992-07-02','Male',9476719974,000003,1,3,'2016-07-25 12:00:00',FALSE),
  (4,'MRS','Dolly','BANTON','11 QUEENSWAY','SCUNTHORPE','S HUMBERSIDE','DN16 2BZ','0121454552','1955-09-18','Female',9476719958,000004,1,2,'2016-07-25 12:00:00',FALSE),
  (5,'MISS','Ruby','MACKIN','3 WILDERSPIN HEIGHTS','BARTON-UPON-HUMBER','S HUMBERSIDE','DN18 5SN','013256541523','1953-01-01','Female',9476719966,000005,2,1,'2016-07-25 12:00:00',FALSE),
  (6,'MISS','Ruby','MACKIN','3 WILDERSPIN HEIGHTS','BARTON-UPON-HUMBER','S HUMBERSIDE','DN18 5SN','013256541523','1953-01-01','Female',9000000068,000006,2,1,'2016-07-25 12:00:00',FALSE),
  (7,'MISS','Ruby','MACKIN','3 WILDERSPIN HEIGHTS','BARTON-UPON-HUMBER','S HUMBERSIDE','DN18 5SN','013256541523','1953-01-01','Female',9000000076,000007,2,1,'2016-07-25 12:00:00',FALSE),
  (8,'MISS','Ruby','MACKIN','3 WILDERSPIN HEIGHTS','BARTON-UPON-HUMBER','S HUMBERSIDE','DN18 5SN','013256541523','1953-01-01','Female',9000000084,000008,2,1,'2016-07-25 12:00:00',FALSE),
  (9,'MRS','Minnie','DAWES','24 GRAMMAR SCHOOL ROAD','BRIGG','','DN20 8AF','01454587554','1952-05-31','Female',9000000092,000009,1,3,'2016-07-25 12:00:00',TRUE),
  (10,'MRS','Minnie','DAWES','24 GRAMMAR SCHOOL ROAD','BRIGG','','DN20 8AF','01454587554','1952-05-31','Female',9000000106,000010,1,3,'2016-07-25 12:00:00',TRUE),
  (11,'MRS','Minnie','DAWES','24 GRAMMAR SCHOOL ROAD','BRIGG','','DN20 8AF','01454587554','1952-05-31','Female',9000000114,000011,1,3,'2016-07-25 12:00:00',FALSE),
  (12,'MRS','Minnie','DAWES','24 GRAMMAR SCHOOL ROAD','BRIGG','','DN20 8AF','01454587554','1952-05-31','Female',9000000122,000012,1,3,'2016-07-25 12:00:00',FALSE),
  (13,'MRS','Minnie','DAWES','24 GRAMMAR SCHOOL ROAD','BRIGG','','DN20 8AF','01454587554','1952-05-31','Female',9000000130,000013,1,3,'2016-07-25 12:00:00',FALSE),
  (15,'MRS','Minnie','DAWES','24 GRAMMAR SCHOOL ROAD','BRIGG','','DN20 8AF','01454587554','1952-05-31','Female',9476718951,000015,1,3,'2016-07-25 12:00:00',TRUE),
  (16,'MR','Jack','DAWES','24 GRAMMAR SCHOOL ROAD','BRIGG','','DN20 8AF','01454587554','1953-08-15','Male',9476719915,000016,1,3,'2016-07-25 12:00:00',FALSE);

UNLOCK TABLES;