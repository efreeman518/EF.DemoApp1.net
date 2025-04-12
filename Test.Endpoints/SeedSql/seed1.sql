--default seet data; note - Cannot insert an explicit value into a timestamp column
insert [todo].[TodoItem] ([Id],[Name],[Status],[IsDeleted])
select '308daef6-c0b6-4134-b8c4-4ca7015a67ce',N'asdfgdfg',2,0 UNION ALL
select '52420339-f1e7-4929-b1cf-42babbb44471',N'fdsaf',2,0 UNION ALL
select 'f531a327-d62d-42cf-9e18-b3324e4e609a',N'fgad8',2,0 UNION ALL
select '195b5f02-7e1b-460b-aadd-b50b37d0d728',N'gadfg',2,0 UNION ALL
select '1c167bd0-bd22-41c0-a54c-3dcbd3ed20ab',N'gagdfg',2,0 UNION ALL
select '22fe0aca-f008-4879-ba73-26fbb52481a1',N'gfdagdfg',2,0;

--[SecureRandom],[SecureDeterministic] fails with error: Operand type clash: varbinary is incompatible with varbinary(8000)
--cast(0x011C3FCACD098F96E6666B5186A2471D29C5D004CD930A05D2FD05CC04E243C72B930D352788D30959B86E5892BC9C643DF8DFB46C7A662ADF599D51DFC0F8B9FD as varbinary(100)),cast(0x01FE83E1F06ED2B70C5A33D357F0B21DAF62CCE6A36C7DA4C04284B4D6068477DB40A7A9EFA699E2E19C70028BD932812D257B9075B3263B2F138879C84828F596 as varbinary(100))