create table if not exists filemetadata(
    id uuid primary key,
    created_at timestamp with time zone not null,
    modified_at timestamp with time zone,
    deleted_at timestamp with time zone,
    is_deleted boolean not null default false,
    name varchar(200)
)