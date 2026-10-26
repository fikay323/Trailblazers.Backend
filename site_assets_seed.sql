-- Trailblazers Site Assets Seed Script
CREATE TABLE IF NOT EXISTS site_assets (
    id uuid NOT NULL PRIMARY KEY,
    key character varying(100) NOT NULL,
    "group" character varying(50) NOT NULL,
    image_url character varying(1000) NOT NULL,
    public_id character varying(256),
    alt_text character varying(300),
    title character varying(200),
    caption character varying(500),
    metadata_json text,
    sort_order integer NOT NULL DEFAULT 0,
    updated_at timestamp with time zone NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_site_assets_key" ON site_assets (key);
CREATE INDEX IF NOT EXISTS "IX_site_assets_group" ON site_assets ("group");

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'brand.logo', 'branding', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg', 'trailblazers/branding/trailblazer', 'Trailblazer Academy Crest', 'Main Site Logo', NULL, NULL, 1, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'seo.og.default', 'seo', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg', 'trailblazers/branding/trailblazer', 'Trailblazer Academy & Edukonsult', 'Global Default OG Image', 'Used across all pages unless a page-specific OG card is set.', NULL, 1, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'seo.og.home', 'seo', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg', 'trailblazers/branding/trailblazer', 'Trailblazer Academy Campus', 'Home Page OpenGraph Image', NULL, NULL, 2, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'seo.og.about', 'seo', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg', 'trailblazers/branding/trailblazer', 'Trailblazer Academy Campus', 'About Page OpenGraph Image', NULL, NULL, 3, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'seo.og.summer_coaching', 'seo', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg', 'trailblazers/branding/trailblazer', 'Trailblazer Academy Summer Coaching Program', 'Summer Coaching OpenGraph Image', 'Featured preview card when summer coaching link is shared on WhatsApp/socials.', NULL, 4, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'seo.og.programs', 'seo', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg', 'trailblazers/branding/trailblazer', 'Students studying at Trailblazer Academy', 'Programs Page OpenGraph Image', NULL, NULL, 5, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'seo.og.gallery', 'seo', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg', 'trailblazers/branding/trailblazer', 'Trailblazer Academy campus facilities', 'Campus Gallery OpenGraph Image', NULL, NULL, 6, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'seo.og.contact', 'seo', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg', 'trailblazers/branding/trailblazer', 'Trailblazer Academy campus — contact us', 'Contact Page OpenGraph Image', NULL, NULL, 7, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'seo.og.exam', 'seo', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg', 'trailblazers/branding/trailblazer', 'UTME CBT Mock Exam at Trailblazer Academy', 'CBT Exam Page OpenGraph Image', NULL, NULL, 8, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'seo.og.register', 'seo', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg', 'trailblazers/branding/trailblazer', 'Register at Trailblazer Academy', 'Register Page OpenGraph Image', NULL, NULL, 9, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'home.hero.1', 'home', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051836/trailblazers/gallery/image-7.jpg', 'trailblazers/gallery/image-7', 'JAMB/CBT Prep', 'JAMB/CBT Prep', 'Complete JAMB CBT training with professional guidance.', NULL, 1, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'home.hero.2', 'home', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051829/trailblazers/gallery/image-1.jpg', 'trailblazers/gallery/image-1', 'WAEC Classroom', 'WAEC Classroom', 'Comprehensive WAEC subject lectures and exam prep.', NULL, 2, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'home.hero.3', 'home', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051830/trailblazers/gallery/image-2.jpg', 'trailblazers/gallery/image-2', 'NECO & GCE Tutoring', 'NECO & GCE Tutoring', 'Intensive GCE & NECO tutoring for excellent results.', NULL, 3, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'home.hero.4', 'home', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051831/trailblazers/gallery/image-3.jpg', 'trailblazers/gallery/image-3', 'Our Classrooms', 'Our Classrooms', 'Active, collaborative learning in a productive environment.', NULL, 4, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'home.hero.5', 'home', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051833/trailblazers/gallery/image-5.jpg', 'trailblazers/gallery/image-5', 'Focused Study', 'Focused Study', 'Quiet spaces dedicated to focused self-study.', NULL, 5, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'home.hero.6', 'home', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051834/trailblazers/gallery/image-6.jpg', 'trailblazers/gallery/image-6', 'Expert Instructors', 'Expert Instructors', 'Learn from experienced tutors dedicated to your academic success.', NULL, 6, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'home.hero.7', 'home', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051836/trailblazers/gallery/image-7.jpg', 'trailblazers/gallery/image-7', 'Subject Counseling', 'Subject Counseling', 'Guidance on choosing the right JAMB and WAEC subject combinations.', NULL, 7, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'home.hero.8', 'home', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051834/trailblazers/gallery/image-6.jpg', 'trailblazers/gallery/image-6', 'Conducive Environment', 'Conducive Environment', 'Comfortable, well-ventilated spaces designed to maximize your focus.', NULL, 8, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'about.hero.background', 'about', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051833/trailblazers/gallery/image-5.jpg', 'trailblazers/gallery/image-5', 'Student illustration', 'About Hero Background Card', NULL, NULL, 1, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'about.hero.foreground', 'about', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051831/trailblazers/gallery/image-3.jpg', 'trailblazers/gallery/image-3', 'Teacher explaining', 'About Hero Foreground Card', NULL, NULL, 2, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'about.objectives.banner', 'about', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051839/trailblazers/gallery/image-10.jpg', 'trailblazers/gallery/image-10', 'Students in classroom', 'Organizational Objectives Banner', NULL, NULL, 3, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'about.cbt_lab', 'about', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051839/trailblazers/gallery/image-10.jpg', 'trailblazers/gallery/image-10', 'Dedicated CBT Lab', 'Dedicated CBT Lab Spotlight', NULL, NULL, 4, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'programs.hero', 'programs', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051830/trailblazers/gallery/image-2.jpg', 'trailblazers/gallery/image-2', 'Classroom setting', 'Programs Hero Visual', NULL, NULL, 1, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'gallery.item.1', 'gallery', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051833/trailblazers/gallery/image-5.jpg', 'trailblazers/gallery/image-5', 'Trailblazer Academy building exterior', 'Building Exterior', NULL, '{"className":"md:row-span-2 h-125"}', 1, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'gallery.item.2', 'gallery', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051829/trailblazers/gallery/image-1.jpg', 'trailblazers/gallery/image-1', 'Students in a modern classroom setting', 'Modern Classroom', NULL, '{"className":"h-62.5"}', 2, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'gallery.item.3', 'gallery', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051830/trailblazers/gallery/image-2.jpg', 'trailblazers/gallery/image-2', 'Students working on coding projects', 'Coding & CBT Projects', NULL, '{"className":"h-62.5"}', 3, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'gallery.item.4', 'gallery', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051831/trailblazers/gallery/image-3.jpg', 'trailblazers/gallery/image-3', 'Student studying in the academy library', 'Academy Library', NULL, '{"className":"md:col-span-2 h-62.5"}', 4, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'gallery.item.5', 'gallery', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051832/trailblazers/gallery/image-4.jpg', 'trailblazers/gallery/image-4', 'Science laboratory equipment and students', 'Science Laboratory', NULL, '{"className":"md:row-span-2 h-75"}', 5, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'gallery.item.6', 'gallery', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051834/trailblazers/gallery/image-6.jpg', 'trailblazers/gallery/image-6', 'Outdoor campus area for students', 'Outdoor Campus', NULL, '{"className":"h-62.5"}', 6, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'gallery.item.7', 'gallery', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051836/trailblazers/gallery/image-7.jpg', 'trailblazers/gallery/image-7', 'Collaborative workspace for group projects', 'Collaborative Workspace', NULL, '{"className":"h-62.5"}', 7, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'gallery.item.8', 'gallery', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051837/trailblazers/gallery/image-8.jpg', 'trailblazers/gallery/image-8', 'Students engaged in a seminar', 'Seminar Hall', NULL, '{"className":"md:col-span-2 h-62.5"}', 8, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'gallery.item.9', 'gallery', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051838/trailblazers/gallery/image-9.jpg', 'trailblazers/gallery/image-9', 'Modern technology lab', 'Technology Lab', NULL, '{"className":"md:col-span-2 h-62.5"}', 9, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'gallery.item.10', 'gallery', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051839/trailblazers/gallery/image-10.jpg', 'trailblazers/gallery/image-10', 'Sports and recreational facilities', 'Sports Facilities', NULL, '{"className":"h-62.5"}', 10, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'gallery.item.11', 'gallery', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051841/trailblazers/gallery/image-11.jpg', 'trailblazers/gallery/image-11', 'Student lounge and relaxation area', 'Student Lounge', NULL, '{"className":"md:col-span-3 h-62.5"}', 11, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'team.member.1', 'team', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051841/trailblazers/team/akintunde-gbadamosi.jpg', 'trailblazers/team/akintunde-gbadamosi', 'Mr. Akintunde Gbadamosi', 'Mr. Akintunde Gbadamosi', 'Proprietor', NULL, 1, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'team.member.2', 'team', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051843/trailblazers/team/tolulope-gbadamosi.jpg', 'trailblazers/team/tolulope-gbadamosi', 'Dr. Tolulope Victoria Gbadamosi', 'Dr. Tolulope Victoria Gbadamosi', 'Proprietress / Founder & Director', NULL, 2, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'team.member.3', 'team', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051844/trailblazers/team/adeshola-andrew.jpg', 'trailblazers/team/adeshola-andrew', 'Mr. Adepoju Adeshola Andrew', 'Mr. Adepoju Adeshola Andrew', 'Head of Academics', NULL, 3, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'team.member.4', 'team', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051845/trailblazers/team/rotimi-akani.jpg', 'trailblazers/team/rotimi-akani', 'Dr. Olurotimi Oladayo Akanni', 'Dr. Olurotimi Oladayo Akanni', 'Management Member / Senior Lecturer', NULL, 4, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'team.member.5', 'team', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051846/trailblazers/team/oluwatoyin-awolola.jpg', 'trailblazers/team/oluwatoyin-awolola', 'Dr. Oluwatoyin Isaiah Awolola', 'Dr. Oluwatoyin Isaiah Awolola', 'Management Member / Senior Lecturer', NULL, 5, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'team.member.6', 'team', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051847/trailblazers/team/louis-odubanjo.jpg', 'trailblazers/team/louis-odubanjo', 'Mr. Louis Odubanjo', 'Mr. Louis Odubanjo', 'Management Member / Administrator', NULL, 6, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'team.member.7', 'team', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051848/trailblazers/team/david-gbadamosi.jpg', 'trailblazers/team/david-gbadamosi', 'Mr. David Erioluwa Gbadamosi', 'Mr. David Erioluwa Gbadamosi', 'Data Analyst & Management Member', NULL, 7, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

INSERT INTO site_assets (id, key, "group", image_url, public_id, alt_text, title, caption, metadata_json, sort_order, updated_at)
VALUES (gen_random_uuid(), 'team.member.8', 'team', 'https://res.cloudinary.com/dssjoxdom/image/upload/v1793051849/trailblazers/team/boluwatife-ayano.jpg', 'trailblazers/team/boluwatife-ayano', 'Boluwatife Ayano', 'Boluwatife Ayano', 'Management Member', NULL, 8, NOW())
ON CONFLICT (key) DO UPDATE SET
    image_url = EXCLUDED.image_url,
    public_id = EXCLUDED.public_id,
    alt_text = EXCLUDED.alt_text,
    title = EXCLUDED.title,
    caption = EXCLUDED.caption,
    metadata_json = EXCLUDED.metadata_json,
    sort_order = EXCLUDED.sort_order,
    updated_at = EXCLUDED.updated_at;

