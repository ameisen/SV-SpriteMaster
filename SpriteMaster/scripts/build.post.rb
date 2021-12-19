#!ruby
require 'pathname'
require 'fileutils'
require 'open3'

def set_const(name, value)
	if value.is_a?(String)
		value += '\\' if value[-1] == '\\'
		value = "'#{value}'"
	end
	eval("#{name} = #{value}")
end
def default_const(name, value) = (set_const(name, value) unless Object.const_defined?(name))
Pathname.prepend Module.new {
	def to_win = self.to_s.gsub('/', '\\')
	def initialize(*args) = super(*args.map{|p| p.gsub('\\', '/')})
}

ARGV.each { |arg|
	case arg
	when /^(.+)[=](.*)$/
		puts "<#{$2}>"
		set_const($1, $2)
	when /^(-|--|\/|)32$/
		set_const(:Copy32, true)
	when /^(-|--|\/|)64$/
		set_const(:Copy64, true)
	else
		raise "Unknown Parameter #{arg}"
	end
}

defined? :SolutionDir
defined? :OutDir
default_const(:Copy32, false)
default_const(:Copy64, !Copy32)
default_const(:Mono, File.join("C:", "Program Files", "Mono", "bin"))
default_const(:GamePath, File.join("C:", "Program Files (x86)", "Steam", "steamapps", "common", "Stardew Valley"))

if Object.const_defined?(:IgnoreModFilePatterns)
	FileIgnorePatterns = IgnoreModFilePatterns.split(',').map(&:strip).map{ |p| Regexp.new(p) }
else
	FileIgnorePatterns = []
end

puts "FileIgnorePatterns: #{FileIgnorePatterns}"

puts "SolutionDir: #{SolutionDir}"
puts "OutDir: #{OutDir}"
puts "32-bit: #{Copy32 ? "enabled" : "disabled"}"
puts "64-bit: #{Copy64 ? "enabled" : "disabled"}"

PrebuiltPaths = [Pathname.new(SolutionDir) + 'Libraries']

puts "Copying prebuilt libraries..."
PrebuiltPaths.each { |directory|
	raise "Prebuilt Path '#{directory}' is not a directory" unless directory.directory?
	Dir.glob(directory + '**' + '*') { |f|
		next if f.include?('x86') && !Copy32
		next if f.include?('32') && !Copy32
		next if f.include?('64') && !Copy64
		dest = Pathname.new(OutDir) + Pathname.new(f).relative_path_from(directory)
		puts "'#{f}' -> '#{dest}'"
		FileUtils.cp(f, dest, preserve: true)
	}
}

def is_dotnet?(path)
	str, stat = Open3.capture2e('file', '-b0', path.to_s)
	return nil unless stat.success?
	return str.include?("assembly")
end

class Library
	attr_reader :path, :name, :ext
	def self.is?(path) = ['.dll', '.so', '.dylib'].include?(File.extname(path).downcase)
	def initialize(path)
		@path = Pathname.new(path)
		@name = @path.basename
		@ext = @path.extname.downcase
		@dotnet = is_dotnet?(path)
	end
	def dotnet? = @dotnet
	def to_s = @path.to_s
end

$libraries = []

puts "Assembling library list..."
Dir.glob(Pathname.new(OutDir) + '**' + '*') { |f|
	next unless Library.is?(f)
	next if f.include?('x86') && !Copy32
	next if f.include?('32') && !Copy32
	next if f.include?('64') && !Copy64
	$libraries << Library.new(f)
}

puts "Stripping prebuilt libraries..."
$libraries.each { |lib|
	next if lib.dotnet?
	
	puts lib
	case lib.ext
		when '.dylib'
			system('llvm-strip', '--discard-all', '--strip-debug', lib.path.to_s) rescue nil
		when '.dll', '.so'
			system('strip', '--discard-all', '--strip-unneeded', lib.path.to_s) rescue nil
			system('llvm-strip', '--discard-all', '--strip-unneeded', lib.path.to_s) rescue nil
	end
}

unless Copy32
	puts "Deleting 32-bit libraries..."
	FileUtils.rm_rf(Pathname.new(OutDir) + 'x86')
end

unless Copy64
	puts "Deleting 64-bit libraries..."
	FileUtils.rm_rf(Pathname.new(OutDir) + 'x64')
end

=begin
puts "Linking .NET assemblies..."
$dotnet_libraries = $libraries.select(&:dotnet?)
unless FileIgnorePatterns.empty?
	$dotnet_libraries.select! { |lib| FileIgnorePatterns.none?{ |p| p =~ lib.name.to_s } }
end

def loud_call(*args)
	puts "< #{args.join(' ')} >"
	system(*args.map(&:to_s))
	return $?
end

puts $dotnet_libraries

loud_call(
	Pathname.new(Mono) + 'mono-api-info',
	'-o', 'api.xml',
	'-d', GamePath,
	'-d', File.join(GamePath, 'smapi-internal'),
	'-d', File.join(GamePath, 'Mods', 'ConsoleCommands'),
	'-d', File.join(GamePath, 'Mods', 'ErrorHandler'),
	*$dotnet_libraries
	#*$dotnet_libraries.flat_map { |e| ['-a', e] }
)

loud_call(
	Pathname.new(Mono) + 'monolinker',
	# '-reference'
	'-b',
	'-v',
	#'--deterministic',
	'--skip-unresolved',
	"-out", "#{Dir.pwd}/test.dll",
	'-u', 'copy',
	'-d', GamePath,
	'-d', File.join(GamePath, 'smapi-internal'),
	'-d', File.join(GamePath, 'Mods', 'ConsoleCommands'),
	'-d', File.join(GamePath, 'Mods', 'ErrorHandler'),
	*$dotnet_libraries.flat_map { |e| ['-a', e] }
)
=end
