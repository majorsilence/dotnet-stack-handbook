.PHONY: help serve site pdf examples test check clean convert

help:
	@echo "make serve    - run the Jekyll site locally on http://127.0.0.1:4000"
	@echo "make site     - build the static site into _site/"
	@echo "make pdf      - build build/dotnet-stack-handbook.pdf with pandoc + LaTeX"
	@echo "make examples - build every example project"
	@echo "make test     - run the example test suites"
	@echo "make check    - structural checks on the chapters"
	@echo "make convert  - regenerate _chapters/ from the original blog post"
	@echo "make clean    - remove build output"

serve:
	bundle exec jekyll serve --livereload

site:
	bundle exec jekyll build --destination _site

pdf:
	./tools/build-pdf.sh

examples:
	dotnet build examples/Examples.slnx --configuration Release

test:
	dotnet test examples/Examples.slnx --configuration Release

check:
	./tools/check-chapters.py

# Only useful if you are re-running the original migration; the Markdown in
# _chapters/ is the source of truth and this will overwrite it, including the
# cross-chapter links and chapter introductions added by hand since the split.
convert:
	./tools/split-post.py ../Dev/_posts/2023-04-07-dotnet-development.md

clean:
	rm -rf _site build .jekyll-cache
	dotnet clean examples/Examples.slnx 2>/dev/null || true
